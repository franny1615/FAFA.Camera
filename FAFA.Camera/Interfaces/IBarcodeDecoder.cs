using FAFA.Camera.Models;

#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#endif

namespace FAFA.Camera.Interfaces;

public interface IBarcodeDecoder
{
    void SetDecodeOptions(BarcodeDecodeOptions options);
    BarcodeResult[] Decode(DecodeDataType data);
}