# NotifyBridge

Chuyển thông báo từ điện thoại **Android** sang điện thoại **Windows 10 Mobile**: tin nhắn, app ngân hàng, hay bất kỳ app nào bạn chọn. Thông báo hiện trên Windows Phone giống như trên một chiếc đồng hồ thông minh.

> Windows 10 Mobile đã hết hỗ trợ từ 14/01/2020. Dự án này dành cho những ai vẫn đang dùng máy đó và muốn nhận thông báo từ điện thoại chính.

## Mục lục

- [Hai chế độ sử dụng](#hai-chế-độ-sử-dụng)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Bắt đầu nhanh với chế độ Local](#bắt-đầu-nhanh-với-chế-độ-local)
- [Bắt đầu nhanh với chế độ Push](#bắt-đầu-nhanh-với-chế-độ-push)
- [Build app Windows Phone và Tự dựng relay server (chế độ Push)](#build-app-windows-phone-và-tự-dựng-relay-server-chế-độ-push)
- [Khắc phục sự cố](#khắc-phục-sự-cố)
- [Quyền riêng tư và bảo mật](#quyền-riêng-tư-và-bảo-mật)
- [Hạn chế đã biết](#hạn-chế-đã-biết)

## Hai chế độ sử dụng

| | Local (cùng Wi-Fi) | Push (mọi nơi) |
| --- | --- | --- |
| Cần cài đặt thêm | Không | Có: tài khoản Microsoft Partner Center và một relay server |
| Hoạt động khi nào | Hai máy cùng Wi-Fi, app Windows Phone phải đang mở và sáng màn hình | Kể cả khi đóng app, tắt màn hình, ngoài Wi-Fi |
| Độ trễ | Tức thì | Tức thì, trừ lần đầu sau khi relay "ngủ" (xem phần hạn chế) |

Bạn có thể dùng cả hai. App Android gửi tới mọi đích bạn đã điền.

> Windows tạm dừng app khi bạn chuyển sang app khác hoặc khóa màn hình, nên chế độ Local ngừng nhận khi đó. Muốn nhận khi tắt màn hình thì dùng Push.

## Cấu trúc dự án

| Thư mục | Nội dung |
| --- | --- |
| `Receiver-WindowsPhone/` | App UWP cho Windows Phone. Có tab Local và tab Push 
| `Relay-Server/` | Relay server viết bằng Node.js, chỉ cần cho chế độ Push |

## Bắt đầu nhanh với chế độ Local

Chế độ này không cần tài khoản hay server nào.

1. Cài app Windows Phone 
2. Mở app, vào tab **Local (same Wi-Fi)**, bật công tắc **Listening**. App hiện một địa chỉ dạng `192.168.1.23:48651`.
3. Cài app Android, cấp quyền truy cập thông báo, chọn app cần chuyển.
4. Trong app Android, điền địa chỉ đó vào ô **Local address**, rồi bật **Forward selected notifications**.
5. Gửi thử một tin nhắn và kiểm tra Windows Phone.

## Bắt đầu nhanh với chế độ Push

**Trên Windows Phone** (tab *Push (anywhere)*):

1. Gõ địa chỉ relay vào ô **Your relay server address**. Địa chỉ server mặc định `https://notifybridge-relay.onrender.com` https://notifybridge-relay.onrender.com (Dùng cho app mặc định, ai tự tạo server thì phải tự build app riêng)
2. Bấm **Set up push notifications**. Thành công thì hiện dòng "Registered" và **mã 6 số**.

**Trên Android:**

1. Bấm **Grant notification access** và bật NotifyBridge. Nếu Android 13 trở lên báo "Restricted setting", vào *Cài đặt → Ứng dụng → NotifyBridge*, bấm dấu ba chấm và chọn **Allow restricted settings**, rồi bật lại.
2. Bấm **Choose apps** và chọn app muốn chuyển.
3. Điền **Relay URL** (địa chỉ relay, không có `/notify` ở cuối) và **Relay code** (mã 6 số).
4. Bật **Forward selected notifications**.
5. Vào cài đặt pin của app và chọn **Không hạn chế (Unrestricted)** để Android không tắt app chạy nền.
---

## Build app windows phone và Tự dựng relay server (chế độ Push)

**Dựng relay server**

Phần này làm **một lần duy nhất**. Dành cho ai muốn nhận thông báo tốc độ cao. Hướng dẫn dùng Render (có gói miễn phí), nhưng relay chạy được trên bất kỳ máy chủ nào có Node.js 18 trở lên.

### Bước 1. Đăng ký tài khoản nhà phát triển Microsoft

1. Vào [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com) và đăng ký tài khoản **Individual** bằng tài khoản Microsoft cá nhân.
2. Hiện Microsoft không thu phí đăng ký với tài khoản cá nhân ở nhiều quốc gia. Xem số tiền hiển thị trên trang đăng ký trước khi trả. Bạn cần giấy tờ tùy thân và ảnh selfie để xác minh.
3. Đăng nhập [Partner Center](https://partner.microsoft.com/dashboard). Đợi vài phút cho tới khi thấy mục **Apps and games**.

### Bước 2. Lấy khóa WNS

1. Trong **Apps and games**, bấm *New product → MSIX or PWA app*, đặt tên (ví dụ `notifybridge`) và bấm *Reserve product name*. Không cần nộp app.
2. Mở app đó, vào *Product management → Product Identity* và copy **Package SID**. Khi dùng, ghi đủ tiền tố `ms-app://` (ví dụ `ms-app://S-1-15-2-...`).
3. Vào *Product management → WNS/MPNS*, bấm link **App Registration portal**. Trong trang Azure mở ra, chọn *Client credentials*, tab **Client secrets**, bấm **+ New client secret**, chọn thời hạn dài nhất rồi **Add**.
4. Copy cột **Value** của secret ngay, vì nó chỉ hiện một lần.

Hai giá trị này sẽ là `WNS_PACKAGE_SID` và `WNS_CLIENT_SECRET`. **Không đưa chúng lên GitHub hay chia sẻ cho ai.**

### Bước 3. Đưa code relay lên mạng

**Cách dùng Render:**

1. Tạo repo GitHub (nên để **Private**) chứa code. Có hai cách:
   - Repo chỉ chứa nội dung thư mục `Relay-Server` (`server.js`, `wns.js`, `package.json`, ...) ở ngay thư mục gốc, hoặc
   - Repo chứa cả dự án, và khi tạo dịch vụ trên Render bạn đặt **Root Directory** là `Relay-Server`.
2. Vào [render.com](https://render.com), đăng nhập bằng GitHub, bấm **New → Web Service** và chọn repo.
3. Điền:
   - **Build Command:** `npm install`
   - **Start Command:** `npm start`
   - **Instance Type:** Free (hoặc gói trả phí nếu muốn tránh bị "ngủ")
4. Mở mục **Environment** và thêm hai biến:

   | Key | Value |
   | --- | --- |
   | `WNS_PACKAGE_SID` | Package SID, có tiền tố `ms-app://` |
   | `WNS_CLIENT_SECRET` | Giá trị secret từ Azure |

5. Bấm **Create Web Service** (hoặc *Save changes* nếu đã tạo) và đợi vài phút.
6. Copy địa chỉ dịch vụ, dạng `https://ten-cua-ban.onrender.com`.

**Máy chủ khác (VPS, Railway, Fly.io, ...):** chạy `npm install` rồi `npm start`, đặt hai biến môi trường như trên. Server lắng nghe cổng trong biến `PORT` (mặc định 3000). Bạn cần để nó chạy sau một địa chỉ **https**.

### Bước 4. Kiểm tra

Mở địa chỉ relay trên trình duyệt. Thấy kết quả như sau là server chạy đúng và đã nhận khóa:

```json
{"ok":true,"wnsConfigured":true,"registeredCodes":0}
```

Nếu `wnsConfigured` là `false`, kiểm tra lại hai biến môi trường (đúng tên, không thừa dấu cách hay dấu nháy).

**Build app Windows phone**

### Bước 1.Cần máy tính Windows với **Visual Studio** (khuyên dùng 2019, 2022 cũng được), cài workload **Universal Windows Platform development** và Windows 10 SDK (ví dụ 10.0.15063). App yêu cầu Windows 10 bản 1607 (10.0.14393) trở lên.

1. Mở `Receiver-WindowsPhone/NotifyBridge.sln`.
2. **Chỉ với chế độ Push:** làm [Bước 5 ở phần dựng relay](#bước-5-gắn-danh-tính-app-windows-phone-với-partner-center) để gắn danh tính app. Chế độ Local không cần bước này.
3. Trên điện thoại, bật *Settings → Update & security → For developers → Developer mode*.
4. Cắm USB, chọn cấu hình **ARM** và target **Device**, rồi bấm F5. (Hoặc build app package ra rồi sideload vào máy cũng được)

### Bước 2. Gắn danh tính app Windows Phone với Partner Center

Đây là bước dễ sai nhất. WNS chỉ giao thông báo khi **danh tính của app trên điện thoại khớp với app bạn đã giữ chỗ tên** ở Partner Center. Nếu không khớp, relay sẽ bị Microsoft từ chối với lỗi `HTTP 403`, dù mọi thứ khác đều đúng.

**2.1. Lấy các giá trị từ Partner Center.** Mở app của bạn, vào *Product management → Product Identity* và ghi lại:

| Giá trị | Dùng cho |
| --- | --- |
| `Package/Identity/Name` | Thuộc tính `Name` của `<Identity>` |
| `Package/Identity/Publisher` (dạng `CN=...`) | Thuộc tính `Publisher` của `<Identity>` |
| `Package/Properties/PublisherDisplayName` | `<PublisherDisplayName>` |
| `Package Family Name (PFN)` | Để đối chiếu ở bước 5.3 |
| `MSA app Id` (ở mục *MSA app*) | Thuộc tính `PhoneProductId` |

**2.2. Sửa `Receiver-WindowsPhone/Package.appxmanifest`.** Chuột phải file, chọn **View Code**, rồi sửa:

```xml
<Identity Name="<Package/Identity/Name>"
          Publisher="<Package/Identity/Publisher>"
          Version="1.0.0.0" />
<mp:PhoneIdentity PhoneProductId="<MSA app Id>"
                  PhonePublisherId="00000000-0000-0000-0000-000000000000" />
```

Trong `<Properties>`, đặt `<PublisherDisplayName>` đúng như trên Partner Center. Phần `PhonePublisherId` giữ nguyên toàn số 0.

Thay vì sửa tay, bạn có thể thử chuột phải project, chọn *Publish → Associate App with the Store*. Nếu Visual Studio báo "The store blocked this account" thì dùng cách sửa tay ở trên.

**2.3. Kiểm tra trong tab Packaging.** Mở `Package.appxmanifest` bằng giao diện. Dòng **Package family name** phải **giống hệt** PFN trên Partner Center. Nếu *Publisher* tự đổi thành giá trị khác (ví dụ `CN=Admin`), đó là do chứng chỉ ký thử đang dùng tên khác:
- Bấm **Choose Certificate... → Create...**
- Ở ô **Publisher common name**, gõ giá trị của Publisher **bỏ phần `CN=`**
- Quay lại kiểm tra Publisher và Package family name

**2.4.Gỡ **mọi** mục NotifyBridge trên điện thoại (nếu bạn đã cài app chung của mình), Trong Visual Studio chọn *Build → Rebuild Solution*, cài lại app mới vào máy

**2.5. Xác nhận trên điện thoại.** Mở tab **Push (anywhere)**, kéo xuống cuối và đọc dòng **App identity**. Nó phải bằng PFN của bạn. Chỉ khi khớp mới bấm **Set up push notifications**.

> Trên máy thử của mình, Push chỉ chạy sau khi làm đủ các bước trên, gồm cả việc đặt `PhoneProductId`. Chưa rõ riêng bước nào là bắt buộc, nên hãy làm đủ cả. Đừng chép đè `Package.appxmanifest` bằng file khác sau khi đã sửa xong.
> Làm xong thì bạn chạy thử như ở phần [Bắt đầu nhanh với chế độ Push](#bắt-đầu-nhanh-với-chế-độ-push) nhưng thay bằng server của bạn
### Các endpoint của relay

| Phương thức | Đường dẫn | Dùng để |
| --- | --- | --- |
| `GET` | `/` | Kiểm tra server và trạng thái WNS |
| `POST` | `/register` | App Windows Phone đăng ký mã 6 số cùng kênh push của nó |
| `POST` | `/notify` | App Android gửi một thông báo tới mã 6 số |

Relay chỉ lưu dữ liệu **trong bộ nhớ**. Khi khởi động lại, nó quên mọi mã, nhưng app Windows Phone sẽ tự đăng ký lại mỗi lần bạn mở app.

---

## Khắc phục sự cố

| Triệu chứng | Cách xử lý |
| --- | --- |
| `wnsConfigured` là `false` | Kiểm tra tên và giá trị của hai biến môi trường, rồi để Render khởi động lại |
| Không đăng ký được push | Địa chỉ relay phải bắt đầu bằng `https://` và mở được trên trình duyệt |
| Log relay báo `HTTP 403` | Danh tính app không khớp Partner Center. Làm lại [Bước 5](#bước-5-gắn-danh-tính-app-windows-phone-với-partner-center) và đối chiếu dòng **App identity** trên điện thoại với PFN |
| Dòng **App identity** là một chuỗi ngẫu nhiên | Điện thoại vẫn chạy gói cũ. Gỡ sạch mọi mục NotifyBridge, Rebuild và cài lại |
| `/notify` báo không tìm thấy mã | Mở app Windows Phone một lần để nó đăng ký lại, vì relay quên mã khi khởi động lại |
| Thông báo đầu tiên đến rất trễ | Relay gói miễn phí đang "ngủ", đợi tối đa khoảng một phút. Các thông báo sau đó đến ngay |
| Push ngừng sau khoảng một tháng | Kênh WNS hết hạn. Mở app Windows Phone một lần để đăng ký lại |
| Danh sách app trên Android trống | Dùng bản mới nhất của app (đã khai báo quyền xem danh sách app cho Android 11 trở lên) |
| Không bật được quyền thông báo trên Android 13+ | Dùng *Allow restricted settings* như ở trên |
| Secret hết hạn | Tạo secret mới ở Azure rồi cập nhật `WNS_CLIENT_SECRET` |

## Quyền riêng tư và bảo mật

- Mọi thứ bạn chọn chuyển, **kể cả thông báo ngân hàng và mã OTP**, sẽ hiện trên màn hình khóa và Action Center của Windows Phone. Chỉ chọn những app bạn chấp nhận hiển thị như vậy.
- Ở chế độ Push, nội dung đi qua relay do bạn tự host và qua dịch vụ push của Microsoft.
- Mã 6 số là thứ duy nhất ngăn người lạ gửi thông báo giả vào máy bạn. Relay có giới hạn số lần gọi theo IP để việc đoán mã chậm đi, nhưng đừng chia sẻ mã.
- Relay chỉ chấp nhận địa chỉ kênh dạng `https://*.notify.windows.com`, để không bị lợi dụng gửi token WNS tới nơi khác.
- Không bao giờ commit `WNS_CLIENT_SECRET` lên GitHub. Chỉ đặt nó trong biến môi trường của host.

## Hạn chế đã biết

- Windows 10 Mobile không còn được cập nhật, và không thể đảm bảo WNS tiếp tục giao push ổn định tới máy Mobile. Chế độ Local là phương án dự phòng luôn dùng được.
- Gói miễn phí của Render ngủ sau khoảng 15 phút không có truy cập.
- Relay lưu mã trong bộ nhớ nên mất khi khởi động lại. Với dùng cá nhân thì không sao, còn nhiều người dùng chung thì sẽ bị chậm.
- Mỗi người dựng relay phải dùng khóa WNS của **chính app mình đăng ký** ở Partner Center. Khóa của người khác không dùng chung được.
