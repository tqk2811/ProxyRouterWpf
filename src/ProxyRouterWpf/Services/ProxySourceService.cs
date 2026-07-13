using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    /// <summary>
    /// Single-user, in-memory port of the original EF-backed ProxySourceService. Operates on the
    /// list held by <see cref="ConfigStore"/> and persists to JSON after every mutation.
    /// </summary>
    public class ProxySourceService : IProxySourceService
    {
        readonly ConfigStore _store;

        public ProxySourceService(ConfigStore store)
        {
            _store = store;
        }

        List<ProxySourceVM> Sources => _store.Config.Sources;
        List<ProxySourceGroupVM> Groups => _store.Config.Groups;

        public IReadOnlyList<ProxySourceVM> ListByGroup(Guid? groupId)
        {
            lock (_store.SyncRoot)
            {
                return Sources
                    .Where(x => x.GroupId == groupId)
                    .OrderBy(x => x.Index)
                    .Select(Clone)
                    .ToList();
            }
        }

        static ProxySourceVM Clone(ProxySourceVM s) => new()
        {
            Id = s.Id,
            GroupId = s.GroupId,
            Address = s.Address,
            Port = s.Port,
            ProxyType = s.ProxyType,
            UserName = s.UserName,
            Password = s.Password,
            Index = s.Index,
        };

        static string? ValidateAddress(string address, ProxyType proxyType)
        {
            var kind = Uri.CheckHostName(address);
            if (kind != UriHostNameType.IPv4 && kind != UriHostNameType.IPv6 && kind != UriHostNameType.Dns)
                return "invalid address (must be IPv4, IPv6, or domain).";
            if (proxyType == ProxyType.Https && kind != UriHostNameType.Dns)
                return "Https requires a domain address (IPv4/IPv6 not allowed).";
            return null;
        }

        int NextIndex(Guid? groupId)
        {
            var max = Sources.Where(x => x.GroupId == groupId)
                .Select(x => (int?)x.Index)
                .DefaultIfEmpty(null)
                .Max();
            return (max ?? -1) + 1;
        }

        bool GroupExists(Guid groupId) => Groups.Any(x => x.Id == groupId);

        public Guid Create(CreateProxySourceVM model)
        {
            lock (_store.SyncRoot)
            {
                var address = model.Address.Trim();
                var addressError = ValidateAddress(address, model.ProxyType);
                if (addressError != null)
                    throw new InvalidOperationException(addressError);

                if (model.GroupId.HasValue && !GroupExists(model.GroupId.Value))
                    throw new InvalidOperationException("Group not found.");

                var entity = new ProxySourceVM
                {
                    Id = Guid.NewGuid(),
                    GroupId = model.GroupId,
                    Address = address,
                    Port = model.Port,
                    ProxyType = model.ProxyType,
                    UserName = string.IsNullOrWhiteSpace(model.UserName) ? null : model.UserName.Trim(),
                    Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
                    Index = NextIndex(model.GroupId),
                };
                Sources.Add(entity);
                _store.Save();
                return entity.Id;
            }
        }

        public BulkCreateProxySourceResultVM BulkCreate(BulkCreateProxySourceVM model)
        {
            lock (_store.SyncRoot)
            {
                var result = new BulkCreateProxySourceResultVM();

                if (model.GroupId.HasValue && !GroupExists(model.GroupId.Value))
                    throw new InvalidOperationException("Group not found.");

                var lines = (model.Lines ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var nextIndex = NextIndex(model.GroupId);
                var entities = new List<ProxySourceVM>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var raw = lines[i].Trim();
                    if (raw.Length == 0) { result.Skipped++; continue; }
                    if (raw.StartsWith('#')) { result.Skipped++; continue; }

                    var effectiveProxyType = model.ProxyType;
                    bool schemeParsed = false;
                    string? schemeUserName = null;
                    string? schemePassword = null;

                    var schemeIdx = raw.IndexOf("://", StringComparison.Ordinal);
                    if (schemeIdx > 0)
                    {
                        var scheme = raw.Substring(0, schemeIdx).Trim();
                        if (!Enum.TryParse<ProxyType>(scheme, ignoreCase: true, out effectiveProxyType)
                            || !Enum.IsDefined(effectiveProxyType))
                        {
                            result.Errors.Add($"Line {i + 1}: unknown proxy scheme '{scheme}'.");
                            continue;
                        }

                        var rest = raw.Substring(schemeIdx + 3).Trim();
                        var atIdx = rest.LastIndexOf('@');
                        if (atIdx >= 0)
                        {
                            var cred = rest.Substring(0, atIdx);
                            if (effectiveProxyType == ProxyType.Socks4)
                            {
                                schemeUserName = cred;
                            }
                            else
                            {
                                var ci = cred.IndexOf(':');
                                if (ci >= 0)
                                {
                                    schemeUserName = cred.Substring(0, ci);
                                    schemePassword = cred.Substring(ci + 1);
                                }
                                else
                                {
                                    schemeUserName = cred;
                                }
                            }
                            raw = rest.Substring(atIdx + 1).Trim();
                        }
                        else
                        {
                            raw = rest;
                        }
                        schemeParsed = true;
                    }

                    var isSocks4 = effectiveProxyType == ProxyType.Socks4;

                    string address;
                    string[] tail;
                    if (raw.StartsWith('['))
                    {
                        var closeIdx = raw.IndexOf(']');
                        if (closeIdx < 0 || closeIdx + 1 >= raw.Length || raw[closeIdx + 1] != ':')
                        {
                            result.Errors.Add($"Line {i + 1}: expected '[ipv6]:port[...]'.");
                            continue;
                        }
                        address = raw.Substring(1, closeIdx - 1).Trim();
                        tail = raw.Substring(closeIdx + 2).Split(':');
                    }
                    else
                    {
                        var parts = raw.Split(':');
                        if (parts.Length < 2)
                        {
                            result.Errors.Add($"Line {i + 1}: expected 'address:port[...]'.");
                            continue;
                        }
                        address = parts[0].Trim();
                        tail = parts.Skip(1).ToArray();
                    }

                    var validTailShape = schemeParsed
                        ? tail.Length == 1
                        : (isSocks4
                            ? (tail.Length == 1 || tail.Length == 2)
                            : (tail.Length == 1 || tail.Length == 3));
                    if (!validTailShape)
                    {
                        var expected = schemeParsed
                            ? "'scheme://[creds@]address:port'"
                            : (isSocks4
                                ? "'address:port' or 'address:port:userid'"
                                : "'address:port' or 'address:port:user:pass'");
                        result.Errors.Add($"Line {i + 1}: expected {expected}.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(address) || address.Length > 253)
                    {
                        result.Errors.Add($"Line {i + 1}: invalid address.");
                        continue;
                    }

                    var addressError = ValidateAddress(address, effectiveProxyType);
                    if (addressError != null)
                    {
                        result.Errors.Add($"Line {i + 1}: {addressError}");
                        continue;
                    }

                    if (!int.TryParse(tail[0].Trim(), out int port) || port < 1 || port > 65535)
                    {
                        result.Errors.Add($"Line {i + 1}: invalid port.");
                        continue;
                    }

                    string? userName = null;
                    string? password = null;
                    if (schemeParsed)
                    {
                        userName = schemeUserName;
                        password = schemePassword;
                    }
                    else if (isSocks4 && tail.Length == 2)
                    {
                        userName = tail[1];
                    }
                    else if (!isSocks4 && tail.Length == 3)
                    {
                        userName = tail[1];
                        password = tail[2];
                    }

                    if (userName != null && userName.Length > 128)
                    {
                        result.Errors.Add($"Line {i + 1}: {(isSocks4 ? "userid" : "username")} too long.");
                        continue;
                    }
                    if (password != null && password.Length > 256)
                    {
                        result.Errors.Add($"Line {i + 1}: password too long.");
                        continue;
                    }

                    entities.Add(new ProxySourceVM
                    {
                        Id = Guid.NewGuid(),
                        GroupId = model.GroupId,
                        Address = address,
                        Port = port,
                        ProxyType = effectiveProxyType,
                        UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
                        Password = string.IsNullOrWhiteSpace(password) ? null : password,
                        Index = nextIndex++,
                    });
                }

                if (entities.Count > 0)
                {
                    Sources.AddRange(entities);
                    result.Created = entities.Count;
                    _store.Save();
                }

                return result;
            }
        }

        public void Update(UpdateProxySourceVM model)
        {
            lock (_store.SyncRoot)
            {
                var entity = Sources.FirstOrDefault(x => x.Id == model.Id);
                if (entity == null) throw new InvalidOperationException("ProxySource not found.");

                var address = model.Address.Trim();
                var addressError = ValidateAddress(address, model.ProxyType);
                if (addressError != null)
                    throw new InvalidOperationException(addressError);

                if (model.GroupId.HasValue && model.GroupId != entity.GroupId && !GroupExists(model.GroupId.Value))
                    throw new InvalidOperationException("Group not found.");

                if (model.GroupId != entity.GroupId)
                    entity.Index = NextIndex(model.GroupId);

                entity.GroupId = model.GroupId;
                entity.Address = address;
                entity.Port = model.Port;
                entity.ProxyType = model.ProxyType;
                entity.UserName = string.IsNullOrWhiteSpace(model.UserName) ? null : model.UserName.Trim();
                entity.Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password;

                _store.Save();
            }
        }

        public int AssignGroup(AssignGroupProxySourceVM model)
        {
            lock (_store.SyncRoot)
            {
                if (model.Ids.Count == 0) return 0;

                if (model.GroupId.HasValue && !GroupExists(model.GroupId.Value))
                    throw new InvalidOperationException("Group not found.");

                var ids = model.Ids.Distinct().ToList();
                var entities = Sources.Where(x => ids.Contains(x.Id)).ToList();

                int changed = 0;
                int nextIndex = entities.Any(e => e.GroupId != model.GroupId) ? NextIndex(model.GroupId) : 0;
                var idOrder = ids.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
                foreach (var entity in entities.OrderBy(e => idOrder.TryGetValue(e.Id, out var i) ? i : int.MaxValue))
                {
                    if (entity.GroupId == model.GroupId) continue;
                    entity.GroupId = model.GroupId;
                    entity.Index = nextIndex++;
                    changed++;
                }

                if (changed > 0)
                    _store.Save();

                return changed;
            }
        }

        public void Reorder(Guid? groupId, IReadOnlyList<Guid> orderedIds)
        {
            lock (_store.SyncRoot)
            {
                if (orderedIds.Count == 0) return;

                var inGroup = Sources.Where(x => x.GroupId == groupId).ToDictionary(x => x.Id);
                int idx = 0;
                foreach (var id in orderedIds)
                {
                    if (inGroup.TryGetValue(id, out var e))
                        e.Index = idx++;
                }
                _store.Save();
            }
        }

        public void Delete(Guid id)
        {
            lock (_store.SyncRoot)
            {
                var removed = Sources.RemoveAll(x => x.Id == id);
                if (removed > 0)
                    _store.Save();
            }
        }

        public int BulkDelete(BulkDeleteProxySourceVM model)
        {
            lock (_store.SyncRoot)
            {
                if (model.Ids.Count == 0) return 0;
                var ids = model.Ids.Distinct().ToHashSet();
                int deleted = Sources.RemoveAll(x => ids.Contains(x.Id));
                if (deleted > 0)
                    _store.Save();
                return deleted;
            }
        }
    }
}
