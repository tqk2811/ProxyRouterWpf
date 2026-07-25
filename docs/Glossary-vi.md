# Bảng thuật ngữ (Glossary)

Giải thích ngắn gọn các thuật ngữ chuyên ngành dùng trong tài liệu và trao đổi về project này.

## Visual tree

Cây các phần tử đồ hoạ (Visual/UIElement) mà WPF thực sự dựng ra để đo đạc, bố trí và vẽ lên
màn hình. Khác với *logical tree* (cây quan hệ cha–con khai báo trong XAML), visual tree chỉ chứa
những phần tử đang thực sự hiển thị. Khi một phần tử bị gỡ khỏi visual tree, WPF bắn sự kiện
`Unloaded`; khi được gắn lại, WPF bắn `Loaded`. `TabControl` mặc định gỡ nội dung của tab cũ khỏi
visual tree mỗi lần chuyển tab, nên `Loaded`/`Unloaded` bắn lặp đi lặp lại suốt vòng đời cửa sổ.

## DispatcherTimer

Bộ đếm giờ của WPF, gọi `Tick` trên đúng luồng UI nên handler được phép chạm vào control. Mặc định
tick được xếp hàng ở mức ưu tiên `DispatcherPriority.Background`, tức là chỉ chạy khi hàng đợi
dispatcher rảnh — nếu UI đang bận (dựng danh sách lớn, vẽ lại nhiều shape) thì tick bị hoãn hoặc
gộp lại, làm dữ liệu hiển thị đứng tạm rồi nhảy bù.

## WMI performance counter

Bộ đếm hiệu năng của Windows truy cập qua WMI (Windows Management Instrumentation). Lớp
`Win32_PerfRawData_Tcpip_NetworkInterface` cung cấp số byte *cộng dồn* của từng card mạng; tốc độ
tức thời phải tự tính bằng cách lấy hiệu hai lần đọc chia cho khoảng thời gian giữa chúng — cùng
cách Task Manager làm.

## Trượt nhịp lấy mẫu (sampling aliasing)

Hiện tượng xảy ra khi hai chu kỳ độc lập gần bằng nhau chạy lệch pha dần: ví dụ luồng nền lấy mẫu
mỗi ~1s và timer UI vẽ lại mỗi 1s. Có nhịp UI rơi vào lúc chưa có mẫu mới (đồ thị đứng yên một
khung), nhịp sau lại gặp hai mẫu cùng lúc (đồ thị nhảy hai bước) — nhìn ra thành giật/đứng dù dữ
liệu vẫn đầy đủ.

## Ring buffer

Mảng có kích thước cố định dùng làm hàng đợi vòng: phần tử mới ghi đè lên phần tử cũ nhất khi mảng
đầy. Ở project này, `NetworkBandwidthCache` dùng ring buffer 60 phần tử để giữ đúng 60 mẫu băng
thông gần nhất mà không cấp phát thêm bộ nhớ.

## Tunnel

Một kết nối TCP từ client đi qua proxy tới đích, tính từ lúc `ProxyServer` nhận socket cho tới lúc
socket đóng. Mỗi tunnel có một `tunnelId` (Guid) do thư viện `TqkLibrary.Proxy` sinh ra, và tương
ứng đúng một dòng log trong tab Logs. Trạng thái của nó (`ProxyTunnelLogState`) được điền dần theo
từng chặng: nhận client → nhận diện giao thức → xác thực → định tuyến → truyền dữ liệu → đóng.

## Live row (dòng log đang chạy)

Dòng log của một tunnel **chưa đóng**: chưa có `EndAt`, trạng thái hiển thị là "Đang hoạt động", và
số byte Upload/Download còn tăng. Khác với dòng đã commit (bất biến, nằm trong FIFO), live row chỉ
là ảnh chụp của `ProxyTunnelLogState` đang sống, được dựng lại mỗi lần truy vấn nên số liệu luôn
mới. Xem [FIFO](Glossary-vi.md#L54).

## FIFO (First In First Out)

Hàng đợi "vào trước ra trước". `InMemoryTunnelLogStore` giữ log trong RAM theo FIFO có giới hạn: khi
vượt quá `Capacity` thì các dòng cũ nhất bị loại bỏ trước. Bounded `Channel` nối proxy với bộ tiêu
thụ log cũng là FIFO, nhưng đầy thì *bỏ luôn bản ghi mới* (`DropWrite`) để không chặn luồng proxy.

## INotifyPropertyChanged

Giao diện chuẩn của .NET để một object báo cho WPF biết thuộc tính vừa đổi giá trị, nhờ đó binding
tự cập nhật UI mà không phải dựng lại danh sách. `ObservableObject` (CommunityToolkit.Mvvm) là bản
cài sẵn; thuộc tính khai báo bằng `[ObservableProperty]` sẽ tự sinh code raise sự kiện này.
