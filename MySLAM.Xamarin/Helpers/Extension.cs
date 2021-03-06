﻿using Android.Graphics;
using System.IO;
using System.Runtime.InteropServices;
using Matrix = Android.Opengl.Matrix;

namespace MySLAM.Xamarin.Helpers
{
    public static class AppConst
    {
        public static void Init()
        {
            RootPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/MySLAM/";
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
            CoreNumber = Java.Lang.Runtime.GetRuntime().AvailableProcessors();
        }

        public static int CoreNumber;
        public static string RootPath;
    }

    public static class MatExtension
    {
        private static float[] _CV2Gl =
            {
                1,0,0,0,
                0,-1,0,0,
                0,0,-1,0,
                0,0,0,1
            };
        private static float scale = 1f;
        public static void ConvertToGL(float[] pose, float[] VMat)
        {
            if (pose == null || pose.Length != 16 || VMat == null)
                return;
            pose[3] *= scale;
            pose[7] *= scale;
            pose[11] *= scale;
            Matrix.MultiplyMM(VMat, 0, pose, 0, _CV2Gl, 0);
            Matrix.TransposeM(VMat, 0, (float[])VMat.Clone(), 0);
        }
    }

    public static class YamlExtension
    {
        private static int MAX_BUFFE_SIZE = 100000;
        public static bool CopyWholeFile(this Stream src, Stream dist)
        {
            try
            {
                if (src.CanRead && dist.CanWrite)
                {
                    byte[] buffer = new byte[MAX_BUFFE_SIZE];
                    int len = 0;
                    while ((len = src.Read(buffer, 0, MAX_BUFFE_SIZE)) > 0)
                    {
                        dist.Write(buffer, 0, len);
                    }
                    dist.Flush();
                    return true;
                }
            }
            finally
            {
                src.Close();
                dist.Close();
            }
            return false;
        }

        public static bool WriteIMUSettings(string file, Org.Opencv.Core.Mat covar, Org.Opencv.Core.Mat mean)
        {
            return _WriteIMUSettings(file, covar.NativeObjAddr, mean.NativeObjAddr);
        }
        public static string Edit(this string s, string key, string value)

        {
            int index;
            if ((index = s.IndexOf(key)) != -1)
            {
                index = s.IndexOf(':', index) + 1;
                s = s.Remove(index, s.IndexOf('\n', index) - index);
                s = s.Insert(index, " " + value);
                return s;
            }
            else
                return null;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "WriteIMUSettings")]
        private static extern bool _WriteIMUSettings(string file, long mataddress1, long mataddress2);
        #endregion
    }

    public static class BitmapExtension
    {
        private static Paint paint = new Paint();

        static BitmapExtension()
        {
            var cm = new ColorMatrix();
            cm.SetSaturation(0);
            var f = new ColorMatrixColorFilter(cm);
            paint.SetColorFilter(f);
        }

        public static Bitmap ToGraySacle(this Bitmap origin)
        {
            var grayscaleBitmap =
                Bitmap.CreateBitmap(origin.Width, origin.Height, Bitmap.Config.Argb8888);
            var c = new Canvas(grayscaleBitmap);
            c.DrawBitmap(origin, 0, 0, paint);
            return grayscaleBitmap;
        }

        public static int Area(this Android.Util.Size size)
        {
            return size.Width * size.Height;
        }
    }
}