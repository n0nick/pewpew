﻿using System;
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
using PewPew.Server;
using Bend.Util;
using Microsoft.Kinect;
using System.Threading;
using System.IO;

namespace PewPew
{
    public partial class MainWindow : Window
    {
        private KinectHttpServer _server;
        private Thread _listenThread;

        private KinectSensor sensor;

        // skeleton
        private DrawingGroup drawingGroup;
        private DrawingImage skeletonImgSource;
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        //color
        private WriteableBitmap colorImgSource;
        private byte[] colorPixels;
        private ColorImageFormat colorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            _listenThread = new Thread(new ThreadStart(StartListening));
            _listenThread.Start();
            lblConnectionStatus.Content = "Listening";
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _server = null;
                _listenThread.Abort();
            }
            catch (Exception ex)
            {
            }

            lblConnectionStatus.Content = "Not listening";
        }

        private void StartListening()
        {
            _server = new KinectHttpServer(KinectHttpServer.SERVER_PORT);
            _server.listen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
            
            if (null != this.sensor)
            {
                this.sensor.ColorStream.Enable(this.colorImageFormat);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorImgSource = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.ColorImage.Source = this.colorImgSource;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                this.drawingGroup = new DrawingGroup();
                this.skeletonImgSource = new DrawingImage(this.drawingGroup);
                this.SkeletonImage.Source = this.skeletonImgSource;
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                /* dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight)); */

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }


        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorImgSource.WritePixels(
                        new Int32Rect(0, 0, this.colorImgSource.PixelWidth, this.colorImgSource.PixelHeight),
                        this.colorPixels,
                        this.colorImgSource.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
        
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

#region CRAP
        //private void InitializeKinectComponents()
        //{
        //    foreach (var potentialSensor in KinectSensor.KinectSensors)
        //    {
        //        if (potentialSensor.Status == KinectStatus.Connected)
        //        {
        //            this._sensor = potentialSensor;
        //            break;
        //        }
        //    }

        //    if (null != this._sensor)
        //    {
        //        this._sensor.SkeletonStream.Enable();
        //        this._sensor.ColorStream.Enable(); // for debug

        //        this._colorPixels = new byte[this._sensor.ColorStream.FramePixelDataLength];
        //        this._colorCoordinates = new ColorImagePoint[this._sensor.DepthStream.FramePixelDataLength];
        //        this._colorBitmap = new WriteableBitmap(this._sensor.ColorStream.FrameWidth, this._sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
        //        this.imgPicture.Source = this._colorBitmap;

        //        this._skeletonData = new Skeleton[this._sensor.SkeletonStream.FrameSkeletonArrayLength];

        //        this._sensor.AllFramesReady += this.SensorAllFramesReady;

        //        // Start the sensor!
        //        try
        //        {
        //            this._sensor.Start();
        //        }
        //        catch (IOException)
        //        {
        //            this._sensor = null;
        //        }
        //    }
        //}

        //private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        //{
        //    using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
        //    {
        //        if (colorFrame != null)
        //        {
        //            // Copy the pixel data from the image to a temporary array
        //            colorFrame.CopyPixelDataTo(this._colorPixels);

        //            // Write the pixel data into our bitmap
        //            this._colorBitmap.WritePixels(
        //                new Int32Rect(0, 0, this._colorBitmap.PixelWidth, this._colorBitmap.PixelHeight),
        //                this._colorPixels,
        //                this._colorBitmap.PixelWidth * sizeof(int),
        //                0);
        //        }
        //    }

        //    using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
        //    {
        //        if (skeletonFrame != null)
        //        {
        //            skeletonFrame.CopySkeletonDataTo(_skeletonData);

        //            foreach (var currSkeleton in _skeletonData)
        //            {
        //                if(currSkeleton.Joints[JointType.HandLeft].Position.X > 0)
        //                {
        //                    var handLeft = currSkeleton.Joints[JointType.HandLeft];
        //                    var handRight = currSkeleton.Joints[JointType.HandRight];

        //                    var center = new SkeletonPoint();
        //                    center.X = (handLeft.Position.X + handRight.Position.X) / 2;
        //                    center.Y = (handLeft.Position.Y + handRight.Position.Y) / 2;
        //                    center.Z = (handLeft.Position.Z + handRight.Position.Z) / 2;

        //                    center.X += currSkeleton.Position.X;
        //                    center.Y += currSkeleton.Position.Y;
        //                    center.Z += currSkeleton.Position.Z;

        //                    if (center.X > 0)
        //                    {
        //                        int k = 1;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
#endregion
    }
}
