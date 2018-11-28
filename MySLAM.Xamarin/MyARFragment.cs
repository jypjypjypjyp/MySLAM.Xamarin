﻿using Android.App;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using MySLAM.Xamarin.MyHelper;
using MySLAM.Xamarin.MyView;
using Org.Opencv.Android;
using Org.Opencv.Core;
using System.Threading.Tasks;

namespace MySLAM.Xamarin
{
    public class MyARFragment : Fragment,
                                ILoaderCallbackInterface,
                                CameraBridgeViewBase.ICvCameraViewListener2
    {
        public JavaCameraView CameraView;

        public MyCalibratorHelper CalibratorHelper { get; set; }

        private Android.Util.Size frameSize;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.ar_fragment, container, false);
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            view.FindViewById<FloatingActionButton>(Resource.Id.fab).Click +=
                (o, e) =>
                {
                    Activity.OpenOptionsMenu();
                };
            CameraView = view.FindViewById<JavaCameraView>(Resource.Id.ar_view);
            CameraView.SetMaxFrameSize(1280, 800);
            CameraView.SetCvCameraViewListener2(this);
            CameraView.Click += OnClick;
        }

        #region OptionMenu
        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.ar, menu);
        }
        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            if (CalibratorHelper == null)
                return;
            if (CalibratorHelper.CameraCalibrator.IsCalibrated)
            {
                menu.FindItem(Resource.Id.action_change_mode).SetVisible(true);
                if (CalibratorHelper.FrameRender is ARFrameRender)
                {
                    menu.SetGroupVisible(Resource.Id.mode_ar, true);
                }
            }
            else
            {
                menu.FindItem(Resource.Id.action_change_mode).SetVisible(false);
            }
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_calibrate:
                    Calibrate();
                    break;
                case Resource.Id.render_comparison:
                    CalibratorHelper.ChangeRenderMode<ComparisonFrameRender>();
                    item.SetChecked(true);
                    break;
                case Resource.Id.render_undistortion:
                    CalibratorHelper.ChangeRenderMode<UndistortionFrameRender>();
                    item.SetChecked(true);
                    break;
                case Resource.Id.render_ar:
                    CalibratorHelper.ChangeRenderMode<ARFrameRender>();
                    item.SetChecked(true);
                    break;
                case Resource.Id.render_none:
                    CalibratorHelper.ChangeRenderMode<PreviewFrameRender>();
                    item.SetChecked(true);
                    break;
            }
            return true;
        }
        #endregion

        private void Calibrate()
        {
            if (CalibratorHelper.CameraCalibrator.IsCalibrated)
            {
                new MyDialog(DialogType.Error, Resources.GetString(Resource.String.not_calibrate))
                {
                    PositiveHandler = (o, e) =>
                    {
                        ((Dialog)o).Dismiss();
                    }
                }.Show(FragmentManager, null);
                CalibratorHelper.ChangeRenderMode<CalibrationFrameRender>();
                CalibratorHelper.CameraCalibrator.IsCalibrated = false;
                return;
            }
            if (CalibratorHelper.CameraCalibrator.CornersBufferSize < 2)
            {
                Toast.MakeText(Activity, Resource.String.more_samples, ToastLength.Short).Show();
                return;
            }
            //Perpare Progress Dialog
            CalibratorHelper.ChangeRenderMode<PreviewFrameRender>();
            new MyDialog(DialogType.Progress, Resources.GetString(Resource.String.please_wait))
                .Show(FragmentManager, "Progress Dialog");

            Task.Run(() => CalibratorHelper.CameraCalibrator.Calibrate())
                .ContinueWith(t =>
                {
                    FragmentManager.FindFragmentByTag<DialogFragment>("Progress Dialog").Dismiss();
                    CalibratorHelper.CameraCalibrator.ClearCorners();
                    string resultMessage = "";
                    if (CalibratorHelper.CameraCalibrator.IsCalibrated)
                    {
                        resultMessage = Resources.GetString(Resource.String.calibration_successful)
                                            + CalibratorHelper.CameraCalibrator.AvgReprojectionError;
                        CalibrationResult.Save(Activity,
                                            CalibratorHelper.CameraCalibrator.CameraMatrix);
                        CalibratorHelper.CameraCalibrator.IsCalibrated = true;
                    }
                    else
                    {
                        resultMessage = Resources.GetString(Resource.String.calibration_unsuccessful);

                    }
                    Toast.MakeText(Activity, resultMessage, ToastLength.Short).Show();
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnClick(object sender, System.EventArgs e)
        {
            if (CalibratorHelper.FrameRender is CalibrationFrameRender)
            {
                if (CalibratorHelper.CameraCalibrator.AddCorners())
                {
                    Toast.MakeText(Activity, Resource.String.add_corners, ToastLength.Short).Show();
                }
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug("OpenCV4Android", "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, Activity, this);
            }
            else
            {
                Log.Debug("OpenCV4Android", "OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }
        public override void OnPause()
        {
            base.OnPause();
            if (CameraView != null)
            {
                CameraView.DisableView();
                HelperManager.CameraHelper.CameraLock.Release();
            }
        }

        #region ICvCameraViewListener2
        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame p0)
        {
            var mat = CalibratorHelper.FrameRender.Render(p0);
            return mat;
        }
        public void OnCameraViewStarted(int width, int height)
        {
            if (frameSize == null || frameSize.Width != width || frameSize.Height != height)
            {
                frameSize = new Android.Util.Size(width, height);
                CalibratorHelper = new MyCalibratorHelper(Activity, width, height);
                if (!CalibratorHelper.CameraCalibrator.IsCalibrated)
                {
                    new MyDialog(DialogType.Error, Resources.GetString(Resource.String.not_calibrate))
                    {
                        PositiveHandler = (o, e) =>
                        {
                            ((Dialog)o).Dismiss();
                        }
                    }.Show(FragmentManager, null);
                }
            }
            SetHasOptionsMenu(true);
        }
        public void OnCameraViewStopped()
        {
        }
        #endregion

        #region ILoaderCallbackInterface
        public void OnManagerConnected(int p0)
        {
            switch (p0)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info("OpenCV4Android", "OpenCV loaded successfully");
                    HelperManager.CameraHelper.CameraLock.WaitOne();
                    CameraView.EnableView();
                    break;
                default:
                    break;
            }
        }
        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {
        }
        #endregion
        
    }
}