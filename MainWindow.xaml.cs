using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.IO;


namespace ConnectKinect
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //中间变量
        private bool Mid = false;
        //kinect
        private KinectSensor kinectSensor = null;
        private ColorFrameReader colorFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private CoordinateMapper coordinateMapper = null;
        private Body[] bodies = null;
        private Body onebody = null;
        private MultiSourceFrameReader multiSourceFrameReader = null;
        private JointType[] jointType = new JointType[]
        {
        JointType.SpineBase,JointType.SpineMid,
        JointType.SpineMid,JointType.SpineShoulder,
        JointType.SpineShoulder,JointType.Neck,
        JointType.Neck,JointType.Head,

        JointType.SpineShoulder,JointType.ShoulderLeft,
        JointType.ShoulderLeft,JointType.ElbowLeft,
        JointType.ElbowLeft,JointType.WristLeft,
        JointType.WristLeft,JointType.HandLeft,
        JointType.HandLeft,JointType.HandTipLeft,
        JointType.HandLeft,JointType.ThumbLeft,

        JointType.SpineShoulder,JointType.ShoulderRight,
        JointType.ShoulderRight,JointType.ElbowRight,
        JointType.ElbowRight,JointType.WristRight,
        JointType.WristRight,JointType.HandRight,
        JointType.HandRight,JointType.HandTipRight,
        JointType.HandRight,JointType.ThumbRight,

        JointType.SpineBase,JointType.HipLeft,
        JointType.HipLeft,JointType.KneeLeft,
        JointType.KneeLeft,JointType.AnkleLeft,
        JointType.AnkleLeft,JointType.FootLeft,

        JointType.SpineBase,JointType.HipRight,
        JointType.HipRight,JointType.KneeRight,
        JointType.KneeRight,JointType.AnkleRight,
        JointType.AnkleRight,JointType.FootRight,
        };
        //调用鼠标的源文件

        POINT p = new POINT();
        [Flags]
        public enum mouseEventFlag : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            Wheel = 0x0800,
            Absolute = 0x8000

        }
        [DllImport("user32.dll", EntryPoint = "mouse_event")]
        public static extern void mouse_event(mouseEventFlag dwFlags, int dx, int dy, int dwData, IntPtr DwExtraInfo);
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT P);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        public void GetMousePosition()
        {

            GetCursorPos(out p);
        }
        public void SetMousePosition()
        {
            SetCursorPos(p.X, p.Y);
        }
        public void OneMouseEvent()
        {
            mouse_event(mouseEventFlag.LeftDown | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
            mouse_event(mouseEventFlag.LeftUp | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
        }
        public void TwiceMouseEvent()
        {
            mouse_event(mouseEventFlag.LeftDown | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
            mouse_event(mouseEventFlag.LeftUp | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
            mouse_event(mouseEventFlag.LeftDown | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
            mouse_event(mouseEventFlag.LeftUp | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
        }
        public void RightMouseEvent()
        {
            mouse_event(mouseEventFlag.RightDown | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
            mouse_event(mouseEventFlag.RightUp | mouseEventFlag.Absolute, 0, 0, 0, IntPtr.Zero);
        }
        public MainWindow()
        {
            InitializeComponent();
            this.bodies = new Body[6];
            this.kinectSensor = KinectSensor.GetDefault();
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            this.multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
            this.multiSourceFrameReader.MultiSourceFrameArrived += multiSourceFrameReader_MultiSourceFrameArrived;
            this.kinectSensor.Open();
        }

        void multiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //将骨骼信息复制到bodies
            MultiSourceFrame msf = e.FrameReference.AcquireFrame();
            if (msf != null)
            {
                using (BodyFrame bodyFrame = msf.BodyFrameReference.AcquireFrame())
                {
                    using (ColorFrame colorFrame = msf.ColorFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                            if (bodies == null)
                            {
                                this.bodies = new Body[bodyFrame.BodyCount];

                            }
                            bodyFrame.GetAndRefreshBodyData(this.bodies);
                            canvas.Children.Clear();
                        }
                    }
                }
            }
            //寻找离kinect最近的人
            try
            {
                for (int i = 0; i < bodies.Length; i++)
                {
                    if (bodies[i] != null)
                    {
                        if (bodies[i].IsTracked == true)
                        {
                            if (onebody == null)
                            {
                                onebody = bodies[i];
                            }
                            else
                            {
                                if (onebody.Joints[JointType.Head].Position.Z > bodies[i].Joints[JointType.Head].Position.Z)
                                {
                                    onebody = bodies[i];
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }

            //给onebody的右手一个企鹅
            if (onebody != null)
            {
                if (onebody.IsTracked)
                {
                    HandState handRightState = onebody.HandRightState;
                    Joint hand = onebody.Joints[JointType.HandRight];
                    if (hand.TrackingState == TrackingState.Tracked)
                    {
                        ColorSpacePoint colorSpacePoint = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(hand.Position);
                        double w, h;
                        w = SystemParameters.PrimaryScreenWidth;
                        h = SystemParameters.PrimaryScreenHeight;
                        colorSpacePoint.X = (int)((colorSpacePoint.X));
                        colorSpacePoint.Y = (int)((colorSpacePoint.Y));
                        SetCursorPos((int)colorSpacePoint.X, (int)colorSpacePoint.Y);
                        GetMousePosition();
                        this.lblMouseMessage.Content = this.p.X.ToString() + this.p.Y.ToString();
                    }
                    switch (handRightState)
                    {
                        case HandState.Closed:
                            {
                                if (Mid)
                                {
                                    OneMouseEvent();
                                    Mid = false;
                                }
                                break;
                            }
                        case HandState.Open:
                            {
                                Mid = true;
                                break;
                            }
                    }
                    //            if (onebody.IsTracked)
                    //{
                    //    Joint hand = onebody.Joints[JointType.HandRight];
                    //    Joint shouder = onebody.Joints[JointType.ShoulderRight];
                    //    if (hand.TrackingState == TrackingState.Tracked)
                    //    {
                    //        ColorSpacePoint colorSpacePoint = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(hand.Position);
                    //        double w, h;
                    //        w = SystemParameters.PrimaryScreenWidth;
                    //        h = SystemParameters.PrimaryScreenHeight;
                    //        colorSpacePoint.X = (int)((colorSpacePoint.X ));
                    //        colorSpacePoint.Y = (int)((colorSpacePoint.Y ));                      
                    //        SetCursorPos((int)colorSpacePoint.X, (int)colorSpacePoint.Y); 
                    //        if ((shouder.Position.Z - hand.Position.Z) >= 0.3)
                    //        {
                    //            OneMouseEvent();
                    //        }
                    //        GetMousePosition();
                    //        this.lblMouseMessage.Content = this.p.X.ToString() + this.p.Y.ToString();
                    //    }

                    //}
                }

            }
        }
    }
}
