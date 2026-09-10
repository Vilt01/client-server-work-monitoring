using EmployeeMonitor.Client.Infrastructure;
using static EmployeeMonitor.Client.Infrastructure.NativeMethods;

namespace EmployeeMonitor.Client.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    public byte[] CaptureAsBmp()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"Invalid screen size: {width}x{height}");

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        IntPtr hOld = SelectObject(hdcMem, hBitmap);

        try
        {
            if (!BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY))
                throw new InvalidOperationException("BitBlt failed");

            // Снимаем bitmap с DC перед GetDIBits
            SelectObject(hdcMem, hOld);

            int dataSize = width * height * 4;
            var pixels = new byte[dataSize];

            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0, // BI_RGB
                    biSizeImage = (uint)dataSize
                }
            };

            int result = GetDIBits(hdcMem, hBitmap, 0, (uint)height, pixels, ref bmi, DIB_RGB_COLORS);
            if (result == 0)
                throw new InvalidOperationException("GetDIBits failed");

            return BuildBmpFile(width, height, pixels);
        }
        finally
        {
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    private static byte[] BuildBmpFile(int width, int height, byte[] pixels)
    {
        // BITMAPFILEHEADER (14) + BITMAPINFOHEADER (40) + pixels
        int dataSize = width * height * 4;
        int fileSize = 14 + 40 + dataSize;
        var buffer = new byte[fileSize];
        int offset = 0;

        // BITMAPFILEHEADER
        buffer[offset++] = (byte)'B';
        buffer[offset++] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(buffer, offset); offset += 4;
        BitConverter.GetBytes((short)0).CopyTo(buffer, offset); offset += 2; // reserved1
        BitConverter.GetBytes((short)0).CopyTo(buffer, offset); offset += 2; // reserved2
        BitConverter.GetBytes(14 + 40).CopyTo(buffer, offset); offset += 4;  // offBits

        // BITMAPINFOHEADER
        BitConverter.GetBytes(40).CopyTo(buffer, offset); offset += 4;       // biSize
        BitConverter.GetBytes(width).CopyTo(buffer, offset); offset += 4;
        BitConverter.GetBytes(height).CopyTo(buffer, offset); offset += 4;
        BitConverter.GetBytes((short)1).CopyTo(buffer, offset); offset += 2;  // biPlanes
        BitConverter.GetBytes((short)32).CopyTo(buffer, offset); offset += 2; // biBitCount
        BitConverter.GetBytes(0).CopyTo(buffer, offset); offset += 4;         // biCompression
        BitConverter.GetBytes(dataSize).CopyTo(buffer, offset); offset += 4;
        BitConverter.GetBytes(0).CopyTo(buffer, offset); offset += 4;         // xPelsPerMeter
        BitConverter.GetBytes(0).CopyTo(buffer, offset); offset += 4;         // yPelsPerMeter
        BitConverter.GetBytes(0).CopyTo(buffer, offset); offset += 4;         // clrUsed
        BitConverter.GetBytes(0).CopyTo(buffer, offset); offset += 4;         // clrImportant

        // pixels
        Buffer.BlockCopy(pixels, 0, buffer, offset, dataSize);

        return buffer;
    }
}