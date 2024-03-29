﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.AllFramesReady -= mykinect_AllFramesReady;
                kinect.Stop();
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private WriteableBitmap _DepthImageBitmap;
        private Int32Rect _DepthImageBitmapRect;
        private int _DepthImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {

                #region 彩色影像串流初始化
                ColorImageStream colorStream = kinect.ColorStream;
                kinect.ColorStream.Enable();
                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgr32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;
                #endregion

                #region 深度影像串流初始化
                DepthImageStream depthStream = kinect.DepthStream;
                kinect.DepthStream.Enable();   
                _DepthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _DepthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _DepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthData.Source = _DepthImageBitmap;
                #endregion

                kinect.AllFramesReady += mykinect_AllFramesReady;

                kinect.Start();
            }
        }

        ColorImageFrame colorframe;
        byte[] colorpixelData;

        DepthImageFrame depthframe;
        short[] depthpixelData;
        DepthImagePixel[] depthPixel;
        ColorImagePoint[] colorpoints;
        void mykinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            #region 基本影像處理
            depthframe = e.OpenDepthImageFrame();          
            colorframe = e.OpenColorImageFrame();

            if (depthframe == null || colorframe == null)
                return;
            
            depthpixelData = new short[depthframe.PixelDataLength];                
            depthframe.CopyPixelDataTo(depthpixelData);
            depthPixel = new DepthImagePixel[depthframe.PixelDataLength] ;
            depthframe.CopyDepthImagePixelDataTo(depthPixel);
            _DepthImageBitmap.WritePixels(_DepthImageBitmapRect, depthpixelData, _DepthImageStride, 0);
            colorpixelData = new byte[colorframe.PixelDataLength];
            colorframe.CopyPixelDataTo(colorpixelData);
            #endregion

            #region 座標轉換與過濾
            colorpoints = new ColorImagePoint[depthpixelData.Length];
            kinect.CoordinateMapper.MapDepthFrameToColorFrame(
                depthframe.Format, 
                depthPixel, 
                colorframe.Format, 
                colorpoints);

            if (depthpixelData != null)
                RangeFilter();
            #endregion

            _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, colorpixelData, _ColorImageStride, 0);

            depthframe.Dispose();
            colorframe.Dispose();
        }
     
        void RangeFilter()
        {
            int min =  kinect.DepthStream.MinDepth;
            int max = kinect.DepthStream.MaxDepth;
            
            for (int i = 0; i < depthpixelData.Length; i++)
            {
                PixelInRange(i, min, max);
            }
        }

        void PixelInRange(int i,int min,int max)
        {
            int depth = depthpixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            if (depth < min || depth > max)
            {
                ColorImagePoint p = colorpoints[i];
                int colorindex = (p.X + p.Y * colorframe.Width) * colorframe.BytesPerPixel;
                colorpixelData[colorindex] = 0x00;
                colorpixelData[colorindex + 1] = 0x00;
                colorpixelData[colorindex + 2] = 0x00;
            }
        }

    }
}
