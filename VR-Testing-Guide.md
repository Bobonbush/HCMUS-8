# Hướng dẫn test VR

Tài liệu này mô tả cách test phần VR của game **mà không cần kính thực tế ảo**, và những gì cần bật khi có kính thật.

## Tổng quan phần VR đã có

- **`VRRig`** (gắn trên object `Player` trong scene, `Mode = Auto`): tự phát hiện kính (HMD). Khi có HMD, nó tự động:
  - chuyển camera sang chế độ tracking (thêm `TrackedPoseDriver`, camera nằm dưới node `VRTrackingOffset`);
  - cho thân người đi theo đầu (body-follow-head, room-scale);
  - thêm binding tay cầm VR lúc chạy và tạm khoá phím bàn phím;
  - khi rút kính (ở chế độ Auto) thì khôi phục lại camera + bàn phím như cũ.
- **OpenXR** được nhúng sẵn trong `Packages/com.unity.xr.openxr` (kèm XR Interaction Toolkit 3.6.0). Không cần cài thêm.
- **Initialize XR on Startup = TẮT** (opt-in): nên chơi desktop bình thường KHÔNG bị ép vào VR. VR chỉ bật khi bạn chủ động khởi động (mock) hoặc cắm kính đã cấu hình.

---

## Cách 1 — Mock VR (stereo, KHÔNG cần kính) — xác nhận pipeline

Dùng để kiểm tra nhanh: OpenXR khởi động được, HMD ảo xuất hiện, VRRig tự chuyển sang VR. **Không tương tác được** (Mock Runtime không có cảm biến chuyển động).

1. Bấm **Play**.
2. Menu **`HCMUS/VR/Start Mock VR (stereo)`**.
3. VRRig tự chuyển sang chế độ VR (camera gắn TrackedPoseDriver, thêm binding tay cầm).
4. Xong thì **`HCMUS/VR/Stop Mock VR`** để tắt.

> Menu này nằm ở `Assets/Scripts/Editor/MockVRMenu.cs`. Nó tự tắt XR Device Simulator trước khi khởi động để tránh hai nguồn head-pose tranh nhau.

**Kết quả mong đợi** (đã kiểm chứng): loader = OpenXRLoader, thiết bị `Head Tracking - OpenXR` xuất hiện, `controller.vrMode = true`, 4 binding VR được thêm, 10 binding bàn phím bị mute; Stop thì mọi thứ khôi phục sạch.

---

## Cách 2 — XR Device Simulator (giả lập lắc đầu + tay cầm bằng chuột/phím)

Đây là cách **"cảm nhận" VR gần nhất mà không cần kính** — bạn điều khiển đầu và hai tay cầm giả lập bằng chuột/bàn phím.

1. Trong scene, tìm object **`XR Device Simulator`** (đang **tắt** mặc định) → **bật active** lên.
2. Bấm **Play**.
3. Điều khiển:
   - Giữ **chuột phải + di chuột**: xoay đầu (nhìn quanh).
   - **W/A/S/D**: di chuyển đầu/thân.
   - Giữ **T** (hoặc **Y**): chuyển sang điều khiển tay trái / tay phải, rồi dùng chuột/phím để cử động tay đó.
   - Panel hiển thị trên màn hình liệt kê đầy đủ phím.
4. Camera sẽ xoay theo "đầu" giả lập → đây là lúc thấy VRRig chạy thật sự.

> Nhớ **tắt lại** XR Device Simulator sau khi test để chơi desktop bình thường.

---

## Cách 3 — Kính thật (Meta Quest, Vive, ...)

Nhân lõi (head tracking + render stereo) chạy được ngay. **Nhưng tay cầm chưa bấm được** cho đến khi bật interaction profile đúng loại kính. Các bước:

1. **Project Settings → XR Plug-in Management → OpenXR** (tab PC/Standalone):
   - **Tick interaction profile đúng loại kính** của bạn (ví dụ Meta Quest → *Oculus Touch Controller Profile* hoặc *Meta Quest Touch Pro Controller Profile*; HTC Vive → *HTC Vive Controller Profile*...). Các profile này đã cài sẵn, chỉ cần tick.
   - **Bỏ tick `Mock Runtime`** (nó là feature dành cho test, có thể chặn runtime thật).
2. **XR Plug-in Management → tick `Initialize XR on Startup`** (hiện đang tắt để chơi desktop). Hoặc để tắt và tự gọi khởi động XR trong code khi vào chế độ VR.
3. Cắm kính, Build & Run (hoặc Play trong editor nếu có OpenXR runtime của kính trên máy).

Sau 2 bước cấu hình trên là cắm kính chạy được: VRRig tự nhận HMD và chuyển sang VR.

---

## Bản đồ điều khiển VR (khi ở chế độ VR)

| Hành động | Nút |
|---|---|
| Di chuyển | Cần analog **tay trái** |
| Chạy (sprint) | **Bấm** cần analog tay trái |
| Nhảy | Nút **primary (A/X)** tay phải |
| Tương tác | **Cò (trigger)** tay phải |
| Xoay nhanh (snap turn) | Cần analog **tay phải** |

---

## Xử lý sự cố

- **Bật Mock VR mà không thấy gì đổi**: đảm bảo đang ở **Play mode** trước khi bấm menu; kiểm tra Console có dòng `MockVR: stereo session started`.
- **Camera bị giật/hai nguồn head-pose**: đảm bảo chỉ một trong hai đang chạy — Mock VR **hoặc** XR Device Simulator, không bật cả hai (menu Mock VR đã tự tắt Simulator giúp).
- **Chơi desktop mà bị ép vào VR**: kiểm tra `Initialize XR on Startup` đang **TẮT**.
- **Tay cầm không phản hồi trên kính thật**: chưa tick interaction profile đúng loại kính (xem Cách 3, bước 1).

---

*Phần VR nằm trong lane của Khoa (movement/VR/anomalies/audio). File script chính: `Assets/Scripts/Player/VRRig.cs`, `Assets/Scripts/Editor/MockVRMenu.cs`.*
