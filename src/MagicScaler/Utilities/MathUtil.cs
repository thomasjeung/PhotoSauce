﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;

#if HWINTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

using static System.Math;

namespace PhotoSauce.MagicScaler
{
	internal static class MathUtil
	{
		private const int ishift = 15;
		private const int iscale = 1 << ishift;
		private const int imax = (1 << ishift + 1) - 1;
		private const int iround = iscale >> 1;
		private const float fscale = iscale;
		private const float ifscale = 1f / fscale;
		private const float fround = 0.5f;
		private const double dscale = iscale;
		private const double idscale = 1d / dscale;
		private const double dround = 0.5;

		public const ushort UQ15Max = imax;
		public const ushort UQ15One = iscale;
		public const ushort UQ15Round = iround;
		public const float FloatScale = fscale;
		public const float FloatRound = fround;
		public const double DoubleScale = dscale;
		public const double DoubleRound = dround;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(this int x, int min, int max) => Min(Max(min, x), max);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double Clamp(this double x, double min, double max) => Min(Max(min, x), max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector<T> Clamp<T>(this Vector<T> x, Vector<T> min, Vector<T> max) where T : unmanaged => Vector.Min(Vector.Max(min, x), max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ClampToUQ15(int x) => (ushort)Min(Max(0, x), UQ15Max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ClampToUQ15One(int x) => (ushort)Min(Max(0, x), UQ15One);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ClampToUQ15One(ushort x) => Min(x, UQ15One);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte ClampToByte(int x) => (byte)Min(Max(0, x), byte.MaxValue);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Fix15(double x) => (int)Round(x * dscale);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static int Fix15(float x) => (int)MathF.Round(x * fscale);
#else
		public static int Fix15(float x) => (int)Round(x * fscale);
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort FixToUQ15One(double x) => ClampToUQ15One((int)(x * dscale + dround));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort FixToUQ15One(float x) => ClampToUQ15One((int)(x * fscale + fround));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte FixToByte(double x) => ClampToByte((int)(x * byte.MaxValue + dround));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte FixToByte(float x) => ClampToByte((int)(x * byte.MaxValue + fround));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double UnFix15ToDouble(int x) => x * idscale;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float UnFix15ToFloat(int x) => x * ifscale;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int UnFix8(int x) => x + (iround >> 7) >> 8;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int UnFix15(int x) => x + iround >> ishift;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int UnFix22(int x) => x + (iround << 7) >> 22;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort UnFixToUQ15(int x) => ClampToUQ15(UnFix15(x));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort UnFixToUQ15One(int x) => ClampToUQ15One(UnFix15(x));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte UnFix15ToByte(int x) => ClampToByte(UnFix15(x));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte UnFix22ToByte(int x) => ClampToByte(UnFix22(x));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DivCeiling(int x, int y) => (x + (y - 1)) / y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PowerOfTwoFloor(int x, int powerOfTwo) => x & ~(powerOfTwo - 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PowerOfTwoCeiling(int x, int powerOfTwo) => x + (powerOfTwo - 1) & ~(powerOfTwo - 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static float Sqrt(this float x) => MathF.Sqrt(x);
#else
		public static float Sqrt(this float x) => (float)Math.Sqrt(x);
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static float Floor(this float x) => MathF.Floor(x);
#else
		public static float Floor(this float x) => (float)Math.Floor(x);
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static float Abs(this float x) => MathF.Abs(x);
#else
		public static float Abs(this float x) => Math.Abs(x);
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static float MaxF(float x, float o) => MathF.Max(x, o);
#else
		public static float MaxF(float x, float o) => x < o ? o : x;
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MATHF
		public static float PowF(float x, float y) => MathF.Pow(x, y);
#else
		public static float PowF(float x, float y) => (float)Pow(x, y);
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Lerp(float l, float h, float d) => h * d + l * (1f - d);

		public static bool IsRoughlyEqualTo(this float x, float y) => (x - y).Abs() < 0.0001f;

		unsafe public static bool IsRouglyEqualTo(this Matrix4x4 m1, Matrix4x4 m2)
		{
			const float epsilon = 0.001f;

#if HWINTRINSICS
			if (Sse.IsSupported)
			{
				var veps = Vector128.Create(epsilon);
				var vmsk = Vector128.Create(0x7fffffff).AsSingle();

				return
					Sse.MoveMask(Sse.CompareNotLessThan(Sse.And(Sse.Subtract(Sse.LoadVector128(&m1.M11), Sse.LoadVector128(&m2.M11)), vmsk), veps)) == 0 &&
					Sse.MoveMask(Sse.CompareNotLessThan(Sse.And(Sse.Subtract(Sse.LoadVector128(&m1.M21), Sse.LoadVector128(&m2.M21)), vmsk), veps)) == 0 &&
					Sse.MoveMask(Sse.CompareNotLessThan(Sse.And(Sse.Subtract(Sse.LoadVector128(&m1.M31), Sse.LoadVector128(&m2.M31)), vmsk), veps)) == 0 &&
					Sse.MoveMask(Sse.CompareNotLessThan(Sse.And(Sse.Subtract(Sse.LoadVector128(&m1.M41), Sse.LoadVector128(&m2.M41)), vmsk), veps)) == 0;
			}
#endif

			var md = m1 - m2;

			return
				md.M11.Abs() < epsilon && md.M12.Abs() < epsilon && md.M13.Abs() < epsilon && md.M14.Abs() < epsilon &&
				md.M21.Abs() < epsilon && md.M22.Abs() < epsilon && md.M23.Abs() < epsilon && md.M24.Abs() < epsilon &&
				md.M31.Abs() < epsilon && md.M32.Abs() < epsilon && md.M33.Abs() < epsilon && md.M34.Abs() < epsilon &&
				md.M41.Abs() < epsilon && md.M42.Abs() < epsilon && md.M43.Abs() < epsilon && md.M44.Abs() < epsilon;
		}
	}
}
