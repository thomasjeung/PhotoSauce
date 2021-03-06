// Copyright © Clinton Ingram and Contributors.  Licensed under the MIT License.

using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using TerraFX.Interop;
using static TerraFX.Interop.Windows;

using PhotoSauce.MagicScaler;

namespace PhotoSauce.Interop.Wic
{
	internal static unsafe class Wic
	{
		private static readonly Lazy<IntPtr> factory = new(() => {
			int hr = S_FALSE;
			using var wicfactory = default(ComPtr<IWICImagingFactory2>);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Before netcoreapp3.0, CoInitializeEx wasn't called on the main thread if a COM apartment model attribute was not present on Main().
				// Checking the current state is enough to trigger the CoInitializeEx call.  https://github.com/dotnet/runtime/issues/10261
				_ = Thread.CurrentThread.GetApartmentState();

				var clsidWicFactory = CLSID_WICImagingFactory2;
				var iidWicFactory = IID_IWICImagingFactory2;
				hr = CoCreateInstance(&clsidWicFactory, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iidWicFactory, (void**)wicfactory.GetAddressOf());
				if (FAILED(hr))
				{
					clsidWicFactory = CLSID_WICImagingFactory1;
					iidWicFactory = IID_IWICImagingFactory;
					if (SUCCEEDED(CoCreateInstance(&clsidWicFactory, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iidWicFactory, (void**)wicfactory.GetAddressOf())))
						throw new PlatformNotSupportedException("The current WIC version is not supported. Please install the Windows platform update. See: https://support.microsoft.com/kb/2670838");
				}
			}

			if (wicfactory.Get() is null)
				throw new PlatformNotSupportedException("Windows Imaging Component (WIC) is not available on this platform.", Marshal.GetExceptionForHR(hr));

			return (IntPtr)wicfactory.Detach();
		});

		public static IWICImagingFactory2* Factory => (IWICImagingFactory2*)factory.Value;

		public static class Metadata
		{
			public const string InteropIndexExif = "/ifd/exif/interop/{ushort=1}";
			public const string InteropIndexJpeg = "/app1" + InteropIndexExif;

			public const string OrientationWindowsPolicy = "System.Photo.Orientation";
			public const string OrientationExif = "/ifd/{ushort=274}";
			public const string OrientationJpeg = "/app1" + OrientationExif;

			public static class Gif
			{
				public const string LogicalScreenWidth = "/logscrdesc/Width";
				public const string LogicalScreenHeight = "/logscrdesc/Height";
				public const string PixelAspectRatio = "/logscrdesc/PixelAspectRatio";
				public const string GlobalPaletteFlag = "/logscrdesc/GlobalColorTableFlag";
				public const string BackgroundColorIndex = "/logscrdesc/BackgroundColorIndex";

				public const string AppExtension = "/appext/application";
				public const string AppExtensionData = "/appext/data";

				public const string FrameLeft = "/imgdesc/Left";
				public const string FrameTop = "/imgdesc/Top";
				public const string FrameWidth = "/imgdesc/Width";
				public const string FrameHeight = "/imgdesc/Height";
				public const string FramePaletteFlag = "/imgdesc/LocalColorTableFlag";

				public const string FrameDelay = "/grctlext/Delay";
				public const string FrameDisposal = "/grctlext/Disposal";
				public const string TransparencyFlag = "/grctlext/TransparencyFlag";
				public const string TransparentColorIndex = "/grctlext/TransparentColorIndex";
			}
		}
	}

	internal static unsafe class WinCodecExtensions
	{
		public static bool RequiresCache(this WICBitmapTransformOptions opt) =>
			opt != WICBitmapTransformOptions.WICBitmapTransformRotate0 && opt != WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;

		public static WICBitmapTransformOptions ToWicTransformOptions(this Orientation o)
		{
			int orientation = (int)o;

			var opt = WICBitmapTransformOptions.WICBitmapTransformRotate0;
			if (orientation == 3 || orientation == 4)
				opt = WICBitmapTransformOptions.WICBitmapTransformRotate180;
			else if (orientation == 6 || orientation == 7)
				opt = WICBitmapTransformOptions.WICBitmapTransformRotate90;
			else if (orientation == 5 || orientation == 8)
				opt = WICBitmapTransformOptions.WICBitmapTransformRotate270;

			if (orientation == 2 || orientation == 4 || orientation == 5 || orientation == 7)
				opt |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;

			return opt;
		}

		public static bool IsSubsampledX(this WICJpegYCrCbSubsamplingOption o) => MiscExtensions.IsSubsampledX((ChromaSubsampleMode)o);

		public static bool IsSubsampledY(this WICJpegYCrCbSubsamplingOption o) => MiscExtensions.IsSubsampledY((ChromaSubsampleMode)o);

		public static WICRect ToWicRect(in this PixelArea a) => new() { X = a.X, Y = a.Y, Width = a.Width, Height = a.Height };

		public static PixelArea ToPixelArea(in this WICRect r) => new(r.X, r.Y, r.Width, r.Height);

		public static T GetValueOrDefault<T>(this ComPtr<IWICMetadataQueryReader> meta, string name) where T : unmanaged
		{
			var pv = default(PROPVARIANT);
			if (FAILED(meta.Get()->GetMetadataByName(name, &pv)))
				return default;

			if (pv.TryGetValue(out T val))
				return val;

			Debug.Print($"VT: {pv.vt} unexpected for type: " + typeof(T).Name, nameof(T));
			HRESULT.Check(PropVariantClear(&pv));

			return default;
		}

		public static Span<T> GetValueOrDefault<T>(this ComPtr<IWICMetadataQueryReader> meta, string name, Span<T> span) where T : unmanaged
		{
			var pv = default(PROPVARIANT);
			if (FAILED(meta.Get()->GetMetadataByName(name, &pv)))
				return default;

			int len = 0;
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
			{
				if (pv.vt is (ushort)VARENUM.VT_BLOB or (ushort)(VARENUM.VT_UI1 | VARENUM.VT_VECTOR) or (ushort)(VARENUM.VT_I1 | VARENUM.VT_VECTOR))
				{
					len = (int)Math.Min(span.Length, pv.Anonymous.blob.cbSize);
					new Span<T>(pv.Anonymous.blob.pBlobData, len).CopyTo(span);
				}
				if (pv.vt == (ushort)VARENUM.VT_LPSTR)
				{
					len = Math.Min(span.Length, new Span<byte>(pv.Anonymous.pszVal, int.MaxValue).IndexOf((byte)'\0'));
					new Span<T>(pv.Anonymous.pszVal, len).CopyTo(span);
				}
			}
			else if (typeof(T) == typeof(char))
			{
				if (pv.vt == (ushort)VARENUM.VT_LPWSTR)
				{
					len = Math.Min(span.Length, new Span<char>(pv.Anonymous.pwszVal, int.MaxValue).IndexOf('\0'));
					new Span<T>(pv.Anonymous.pwszVal, len).CopyTo(span);
				}
				if (pv.vt == (ushort)VARENUM.VT_LPSTR)
				{
					var str = new string(pv.Anonymous.pszVal);
					len = Math.Min(span.Length, str.Length);
					MemoryMarshal.Cast<char, T>(str.AsSpan()).Slice(0, len).CopyTo(span);
				}
			}
			else
			{
				throw new ArgumentException("Marshaling not implemented for type: " + typeof(T).Name, nameof(T));
			}

			HRESULT.Check(PropVariantClear(&pv));

			return span.Slice(0, len);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryGetValue<T>(in this PROPVARIANT pv, out T val) where T : unmanaged
		{
			if (typeof(T) == typeof(bool))
			{
				if (pv.vt == (ushort)VARENUM.VT_BOOL)
				{
					val = (T)(object)(pv.Anonymous.boolVal != 0);
					return true;
				}
			}
			else if (typeof(T) == typeof(byte))
			{
				if (pv.vt == (ushort)VARENUM.VT_UI1)
				{
					val = (T)(object)pv.Anonymous.bVal;
					return true;
				}
			}
			else if (typeof(T) == typeof(ushort))
			{
				if (pv.vt == (ushort)VARENUM.VT_UI2)
				{
					val = (T)(object)pv.Anonymous.uiVal;
					return true;
				}
			}
			else
			{
				throw new ArgumentException("Marshaling not implemented for type: " + typeof(T).Name, nameof(T));
			}

			val = default;
			return false;
		}

		public static void SetValue<T>(this ComPtr<IWICMetadataQueryWriter> meta, string name, T val) where T : unmanaged
		{
			var pv = default(PROPVARIANT);

			if (typeof(T) == typeof(bool))
			{
				pv.vt = (ushort)VARENUM.VT_BOOL;
				pv.Anonymous.boolVal = (short)((bool)(object)val ? -1 : 0);
			}
			else if (typeof(T) == typeof(byte))
			{
				pv.vt = (ushort)VARENUM.VT_UI1;
				pv.Anonymous.bVal = (byte)(object)val;
			}
			else if (typeof(T) == typeof(ushort))
			{
				pv.vt = (ushort)VARENUM.VT_UI2;
				pv.Anonymous.uiVal = (ushort)(object)val;
			}
			else
			{
				throw new ArgumentException("Marshaling not implemented for type: " + typeof(T).Name, nameof(T));
			}

			HRESULT.Check(meta.Get()->SetMetadataByName(name, &pv));
		}

		public static void Write<T>(this ComPtr<IPropertyBag2> bag, string name, T val) where T : unmanaged
		{
			var pvar = default(VARIANT);

			if (typeof(T) == typeof(bool))
			{
				pvar.vt = (ushort)VARENUM.VT_BOOL;
				pvar.Anonymous.iVal = (short)((bool)(object)val ? -1 : 0);
			}
			else if (typeof(T) == typeof(byte))
			{
				pvar.vt = (ushort)VARENUM.VT_UI1;
				pvar.Anonymous.bVal = (byte)(object)val;
			}
			else if (typeof(T) == typeof(float))
			{
				pvar.vt = (ushort)VARENUM.VT_R4;
				pvar.Anonymous.fltVal = (float)(object)val;
			}
			else
			{
				throw new ArgumentException("Marshaling not implemented for type: " + typeof(T).Name, nameof(T));
			}

			fixed (char* pname = name)
			{
				var prop = new PROPBAG2 { pstrName = (ushort*)pname };
				HRESULT.Check(bag.Get()->Write(1, &prop, &pvar));
			}
		}
	}
}

