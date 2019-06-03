﻿using DirectShowLib;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabletCamStreamer
{
    public delegate void NewFrameAvailableHandler(object sender, Bitmap frameData);
    public class CamRetriever
    {
        const int UPDATE_INTERVAL = 50;
        public event NewFrameAvailableHandler NewFrameAvailableEvent;
        protected VideoCapture camCapture;
        private DateTime lastUpdate;
        public int CamIndex { get; set; }
        public Bitmap CurrentFrame { get; set; }
        private int frameWidth = 640;
        private int frameHeight = 480;
        private bool isRunning = false;
        public RectangleF CropArea { get; set; }
        ImageROIExtractor _roiExtractor = null;
        SkinExtractor _skinExtractor = null;
        public ImageROIExtractor RoiExtractor
        {
            get
            {
                return _roiExtractor;
            }

            set
            {
                _roiExtractor = value;
            }
        }

        public SkinExtractor SkinExtractor
        {
            get
            {
                return _skinExtractor;
            }

            set
            {
                _skinExtractor = value;
            }
        }

        public CamRetriever(int camIndex, int frameW = 0, int frameH = 0)
        {
            CamIndex = camIndex;
            lastUpdate = DateTime.Now;
            frameWidth = frameW;
            frameHeight = frameH;
            CropArea = new RectangleF(0.4f, 0.4f, 0.1f, 0.25f);
            //CropArea = new RectangleF(0,0,1,1);
        }
        public void Start()
        {

            if (CamIndex >= 0)
            {
                camCapture = new VideoCapture(CamIndex);
                if (frameWidth > 0)
                {
                    camCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, frameWidth);
                }
                if (frameHeight > 0)
                {
                    camCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, frameHeight);
                }
                if (camCapture != null && camCapture.Ptr != IntPtr.Zero)
                {
                    camCapture.ImageGrabbed += ProcessFrame;
                    camCapture.Start();
                    isRunning = true;
                }
            }
        }
        public void Close()
        {
            isRunning = false;
            if (camCapture != null)
            {
                camCapture.Stop();
                camCapture.Dispose();
            }
        }
        Mat originFrame = new Mat();
        Mat extractedFrame;
        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (camCapture != null && camCapture.Ptr != IntPtr.Zero)
            {
                if ((DateTime.Now - lastUpdate).TotalMilliseconds < UPDATE_INTERVAL)
                {
                    return;
                }
                try
                {
                    camCapture.Retrieve(originFrame, 0);
                    CvInvoke.Flip(originFrame, originFrame, Emgu.CV.CvEnum.FlipType.Horizontal);
                    
                    if(_roiExtractor != null)
                    {
                        extractedFrame = _roiExtractor.extractROI(originFrame);
                        if(_skinExtractor != null)
                        {
                            extractedFrame = _skinExtractor.extractSkinPart(extractedFrame);
                        }
                    }
                    else
                    {
                        extractedFrame = originFrame.Clone();
                    }
                    Bitmap bmp = extractedFrame.Bitmap;
                    /*if (CropArea != null)
                    {
                        bmp = Utilities.CropBitmap(bmp, CropArea.Left, CropArea.Top, CropArea.Width, CropArea.Height);
                    }*/
                    CurrentFrame = bmp;
                    lastUpdate = DateTime.Now;
                    if (NewFrameAvailableEvent != null)
                    {
                        NewFrameAvailableEvent(this, bmp);
                    }
                }
                catch
                {

                }

            }
        }
        public IEnumerable<Image> GrabFrames()
        {
            while (isRunning)
            {
                yield return CurrentFrame;
            }
            yield break;
        }
        public static string[] getCameraList()
        {
            DsDevice[] capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            string[] cameraNames = new string[capDevices.Length];
            for (int i = 0; i < capDevices.Length; i++)
            {
                cameraNames[i] = capDevices[i].Name;
            }
            return cameraNames;
        }
    }
}