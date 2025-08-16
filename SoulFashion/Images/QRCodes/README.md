# Hướng dẫn sử dụng QR Code cho Bank Transfer

## 📱 **Cách lấy QR Code từ app Vietcombank:**

1. **Mở app Vietcombank** trên điện thoại
2. **Đăng nhập** vào tài khoản
3. **Chọn "Mã QR nhận tiền"**
4. **Chọn "Thêm số tiền"** và nhập số tiền đơn hàng
5. **Chụp màn hình** QR code này
6. **Lưu file** vào thư mục này với tên `vietcombank_qr.png`

## 🎯 **Lưu ý:**
- QR code này sẽ có logo Vietcombank, VIETQR, napas 247
- Có thể quét trực tiếp bằng app ngân hàng khác
- Format chuẩn quốc gia Việt Nam

## 📁 **Cấu trúc thư mục:**
```
Images/
└── QRCodes/
    ├── README.md
```    └── vietcombank_qr.png (QR code từ app Vietcombank)


## 🔄 **Cách sử dụng:**
- API sẽ trả về đường dẫn `/Images/QRCodes/vietcombank_qr.png`
- Frontend có thể hiển thị QR code này cho khách hàng
- Khách hàng quét QR code bằng app ngân hàng để chuyển tiền
