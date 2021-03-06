﻿using Android.Opengl;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class Earth : GLBall
    {
        private readonly float z;
        public Earth(float z) : base(0.5f, 5, "earth.png")
        {
            this.z = z;
        }

        private readonly float[] _MMat = new float[16];
        private readonly float[] _MVPMat = new float[16];
        protected override float[] UpdateMVPMat(float[] VPMat)
        {
            Matrix.SetIdentityM(_MMat, 0);
            Matrix.TranslateM(_MMat, 0, 0, 0, z);
            Matrix.MultiplyMM(_MVPMat, 0, VPMat, 0, _MMat, 0);
            return _MVPMat;
        }
    }
}