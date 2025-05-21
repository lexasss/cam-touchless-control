using OpenCvSharp;

namespace CameraTouchlessControl;

public static class CameraDeviceEnumerator
{
    public static Camera[] Get()
    {
        var cameras = new List<Camera>();

        //var frame = new Mat();

        var cameraNames = CameraNameEnumerator.Get();
        int cameraNameIndex = 0;

        // Searching for cameras IDs...
        for (int drv = 0; drv < _drivers.Length; drv++)
        {
            string driverName = _drivers[drv].EnumName;
            int driverEnum = _drivers[drv].EnumValue;
            //var driverComment = drivers[drv].comment;

            // Testing the driver...
            bool found = false;

            for (int idx = 0; idx < MaxID; idx++)
            {
                var cap = new VideoCapture(driverEnum + idx);  // open the camera
                if (cap.IsOpened())
                {
                    found = true;

                    var cameraName = $"{driverName}+{idx}";
                    if (cameraNameIndex < cameraNames.Length)
                    {
                        cameraName = cameraNames[cameraNameIndex++];
                    }

                    cameras.Add(new Camera(driverEnum + idx, cameraName));  // vector of all available cameras

                    //cap.Read(frame);
                    //System.Diagnostics.Debug.WriteLine($"{driverName}+{idx}\t opens: OK \t grabs: " + (frame.Empty() ? "FAIL" : "OK"));
                }
                cap.Release();
            }

            if (!found)
                System.Diagnostics.Debug.WriteLine($"{driverName}: no cameras");

        }

        System.Diagnostics.Debug.WriteLine($"{cameras.Count} camera IDs has been found ");

        return cameras.ToArray();
    }

    // Internal

    const int MaxID = 100; // 100 IDs between drivers

    struct CapDriver
    {
        public int EnumValue;
        public string EnumName;
        public string Comment;
    };

    static readonly CapDriver[] _drivers =
        [
           // list of all CAP drivers (see highgui_c.h)
           /*new() { EnumValue = (int)VideoCaptureAPIs.ANDROID, EnumName = "Android", Comment = "Android" },
            new() { EnumValue = (int)VideoCaptureAPIs.ARAVIS, EnumName = "Aravis", Comment = "Aravis SDK" },
            new() { EnumValue = (int)VideoCaptureAPIs.AVFOUNDATION, EnumName = "AVFoundation", Comment = "AVFoundation framework for iOS (OS X Lion will have the same API)" },
            new() { EnumValue = (int)VideoCaptureAPIs.CAP_UEYE, EnumName = "CapUEye", Comment = "CapUEye" },
            new() { EnumValue = (int)VideoCaptureAPIs.CMU1394, EnumName = "CMU1394", Comment = "CMU1394" },
            new() { EnumValue = (int)VideoCaptureAPIs.DC1394, EnumName = "DC1394", Comment = "DC1394" },
            */
            new() { EnumValue = (int)VideoCaptureAPIs.DSHOW, EnumName = "DSHOW", Comment = "DirectShow (via videoInput)" },
            /*
            new() { EnumValue = (int)VideoCaptureAPIs.FFMPEG, EnumName = "FFMPEG", Comment = "Open and record video file or stream using the FFMPEG library" },
            new() { EnumValue = (int)VideoCaptureAPIs.FIREWARE, EnumName = "FireWare", Comment = "IEEE 1394 drivers" },
            new() { EnumValue = (int)VideoCaptureAPIs.FIREWIRE, EnumName = "FireWire", Comment = "IEEE 1394 drivers" },
            new() { EnumValue = (int)VideoCaptureAPIs.GIGANETIX, EnumName = "Giganetix", Comment = "Smartek Giganetix GigEVisionSDK" },
            new() { EnumValue = (int)VideoCaptureAPIs.GPHOTO2, EnumName = "GPhoto2", Comment = "gPhoto2 connection" },
            new() { EnumValue = (int)VideoCaptureAPIs.GSTREAMER, EnumName = "GStreamer", Comment = "GStreamer" },
            new() { EnumValue = (int)VideoCaptureAPIs.IEEE1394, EnumName = "IEEE1394", Comment = "IEEE 1394 driver" },
            new() { EnumValue = (int)VideoCaptureAPIs.IMAGES, EnumName = "Images", Comment = "OpenCV Image Sequence (e.g. img_%02d.jpg)" },
            new() { EnumValue = (int)VideoCaptureAPIs.INTELPERC, EnumName = "IntelPERC", Comment = "Intel Perceptual Computing SDK" },
            new() { EnumValue = (int)VideoCaptureAPIs.INTEL_MFX, EnumName = "IntelMFX", Comment = "Intel MFX driver" },
            new() { EnumValue = (int)VideoCaptureAPIs.MSMF, EnumName = "MSMF", Comment = "Microsoft Media Foundation (via videoInput)" },
            new() { EnumValue = (int)VideoCaptureAPIs.OPENCV_MJPEG, EnumName = "OpenCvMJpeg", Comment = "OpenCV MJPEG" },
            new() { EnumValue = (int)VideoCaptureAPIs.OPENNI, EnumName = "OpenNI", Comment = "OpenNI(for Kinect) " },
            new() { EnumValue = (int)VideoCaptureAPIs.OPENNI_ASUS, EnumName = "OpenNI_ASUS", Comment = "OpenNI(for Asus Xtion) " },
            new() { EnumValue = (int)VideoCaptureAPIs.OPENNI2, EnumName = "OpenNI2", Comment = "OpenNI2 (for Kinect)" },
            new() { EnumValue = (int)VideoCaptureAPIs.OPENNI2_ASUS, EnumName = "OpenNI2_ASUS", Comment = "OpenNI2 (for Asus Xtion and Occipital Structure sensors)" },
            new() { EnumValue = (int)VideoCaptureAPIs.PVAPI, EnumName = "PVAPI", Comment = "PvAPI, Prosilica GigE SDK" },
            new() { EnumValue = (int)VideoCaptureAPIs.REALSENSE, EnumName = "Realsense", Comment = "Realsense drivers" },
            new() { EnumValue = (int)VideoCaptureAPIs.V4L, EnumName = "V4L", Comment = "platform native" },
            new() { EnumValue = (int)VideoCaptureAPIs.V4L2, EnumName = "V4L2", Comment = "platform native" },
            new() { EnumValue = (int)VideoCaptureAPIs.WINRT, EnumName = "WinRT", Comment = "Microsoft Windows Runtime using Media Foundation" },
            new() { EnumValue = (int)VideoCaptureAPIs.XIAPI, EnumName = "XIAPI", Comment = "XIMEA Camera API" },
            new() { EnumValue = (int)VideoCaptureAPIs.XINE, EnumName = "XINE", Comment = "XINE" },
            */
        ];
}