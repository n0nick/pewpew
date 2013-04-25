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
using PewPew.Game;
using Bend.Util;
using Microsoft.Kinect;
using System.Threading;
using System.IO;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfAnimatedControl;

namespace PewPew
{
    public partial class MainWindow : Window
    {
        private KinectHttpServer _server;
        private Thread _listenThread;

        private KinectSensor sensor;
        private MyGame currGame;

        // skeleton
        private DrawingGroup drawingGroup;
        private DrawingImage skeletonImgSource;
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 3;
        private const double PlayerThickness = 8;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;
        private readonly Brush playerPointBrush = Brushes.Violet;
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        //color
        private WriteableBitmap colorImgSource;
        private byte[] colorPixels;
        private ColorImageFormat colorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;

        private BitmapImage crosshairBitmap = new BitmapImage(new Uri((@"../../images/crosshair.png"), UriKind.Relative));

        private AnimatedImage explosionGif = new AnimatedImage() { Source = AnimatedImage.CreateBitmapSourceFromBitmap(new System.Drawing.Bitmap(@"../../images/anim_explode.gif")) };

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        //private void btnStart_Click(object sender, RoutedEventArgs e)
        //{
        //    _listenThread = new Thread(new ThreadStart(StartListening));
        //    _listenThread.Start();
        //    lblConnectionStatus.Content = "Listening";
        //}

        //private void btnStop_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        _server = null;
        //        _listenThread.Abort();
        //    }
        //    catch (Exception ex)
        //    {
        //    }

        //    lblConnectionStatus.Content = "Not listening";
        //}

        private void StartListening()
        {
            _server = new KinectHttpServer(KinectHttpServer.SERVER_PORT) { player = currGame.player };
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

                initGame();
            }
        }

        private void initGame()
        {            
            VideoControl.Play();
            VideoControl.Volume = 0.1;

            currGame = new MyGame();
            this.HealthBarImage.Source = new BitmapImage(new Uri((@"../../images/3livesleft.png"), UriKind.Relative));

            // init targets
            TimeSpan[] targetTriggers = new TimeSpan[9];
            targetTriggers[0] = new TimeSpan(0, 0, 5);
            targetTriggers[1] = new TimeSpan(0, 0, 20);
            targetTriggers[2] = new TimeSpan(0, 0, 40);
            targetTriggers[3] = new TimeSpan(0, 1, 0);
            targetTriggers[4] = new TimeSpan(0, 1, 20);
            targetTriggers[5] = new TimeSpan(0, 1, 47);
            targetTriggers[6] = new TimeSpan(0, 2, 3);
            targetTriggers[7] = new TimeSpan(0, 2, 25);
            targetTriggers[8] = new TimeSpan(0, 2, 45);

            // Server Start
            _listenThread = new Thread(new ThreadStart(StartListening));
            _listenThread.Start();

            

            // clip video to show top bar
            VideoControl.Clip = new RectangleGeometry(new Rect(new System.Windows.Point(0, 420), new System.Windows.Point(2820, 1580)));

            // init combination image & blinking action
            DoubleAnimation targetFader = new DoubleAnimation();
            targetFader.From = 1.0;
            targetFader.To = 0.0;
            targetFader.Duration = new Duration(TimeSpan.FromSeconds(1));
            targetFader.AutoReverse = true;
            targetFader.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard myStoryboard = new Storyboard();
            myStoryboard.Children.Add(targetFader);
            Storyboard.SetTargetProperty(targetFader, new PropertyPath(System.Windows.Shapes.Rectangle.OpacityProperty));
            this.RegisterName("combToFade", this.CombinationImage);
            Storyboard.SetTargetName(targetFader, "combToFade");

            //// init explosion image & blinking action
            // Image explosion = new Image();
            // explosion.Source = new BitmapImage(new Uri((@"../../images/anim_explode.gif"), UriKind.Relative));

            soundPlayer.LoadedBehavior = MediaState.Manual;
            soundPlayer.ScrubbingEnabled = true;
            soundPlayer.Source = new Uri((@"../../sounds/explosion.mp3"), UriKind.Relative);

            winningSoundPlayer.LoadedBehavior = MediaState.Manual;
            winningSoundPlayer.ScrubbingEnabled = true;
            winningSoundPlayer.Source = new Uri((@"../../sounds/win.mp3"), UriKind.Relative);
            winningSoundPlayer.Volume = 10;
            

            //DoubleAnimation explosionFader = new DoubleAnimation();
            //explosionFader.From = 0.0;
            //explosionFader.To = 1.0;
            //explosionFader.Duration = new Duration(TimeSpan.FromSeconds(0.2));
            //explosionFader.AutoReverse = true;
            //explosionFader.RepeatBehavior = RepeatBehavior.Forever;
            //Storyboard explosionBoard = new Storyboard();
            //explosionBoard.Children.Add(explosionFader);
            //Storyboard.SetTargetProperty(explosionFader, new PropertyPath(System.Windows.Shapes.Rectangle.OpacityProperty));
            //this.RegisterName("explode", explosion);
            //Storyboard.SetTargetName(explosionFader, "explode");

            // generate new hitpoint
            Image crosshair = new Image()
            {
                Width = 128,
                Height = 128,
                Source = this.crosshairBitmap,
            };
            
            // random enemy helper
            Random randCombination = new Random();

            // summon a target
            DispatcherTimer targetSummoner = new DispatcherTimer(new TimeSpan(0, 0, 0, 1), DispatcherPriority.Normal, delegate
            {
                TimeSpan checkTime = VideoControl.Position;

                if (!currGame.targetAppears && (currGame.currTargetIndex < targetTriggers.Length) && (checkTime.Seconds == targetTriggers[currGame.currTargetIndex].Seconds))
                {
                    //lblQrText.Content = currGame.currTargetIndex;
                    int currCombination = randCombination.Next(0, Enum.GetNames(typeof(Target.TargetName)).Length);
                    //lblQrText.Content = Target.EnemyTypes[(Target.TargetName)currCombination].inputText; // make random
                    currGame.currTarget = Target.EnemyTypes[(Target.TargetName)currCombination];
                    this.CombinationImage.Source = new BitmapImage(new Uri(@"../../images/" + Target.EnemyTypes[(Target.TargetName)currCombination].fileName, UriKind.Relative));
                    this.CombinationImage.Visibility = System.Windows.Visibility.Visible;
                    
                    currGame.targetAppears = true;

                    myStoryboard.Begin(this);
                }

                if (currGame.explosion)
                {
                    //currGame.crosshair.Source = this.crosshairBitmap;
                    currGame.explosion = false;
                    
                    explosionGif.StopAnimate();
                    PlayCanvas.Children.Remove(explosionGif);
                }

                if (currGame.targetAppears)
                {
                    if (currGame.currTargetSecCounter < 9)
                    {
                        currGame.currTargetSecCounter++;
                    }
                    else
                    {
                        currGame.score -= 5000;
                        lblScore.Content = currGame.score;
                        PlayCanvas.Children.Remove(currGame.crosshair);
                        RemoveTarget();

                    }
                }

            }, this.Dispatcher);

            // moving target
            DispatcherTimer videoPanning = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 5), DispatcherPriority.Normal, delegate
            {
                if (currGame.goingLeft)
                {
                    VideoControl.Arrange(new Rect(new System.Windows.Point(currGame.pixelCount--, 0), new System.Windows.Point(2820, 1580)));
                    if (currGame.pixelCount < -1500)
                    {
                        currGame.goingLeft = false;
                    }
                }
                else
                {
                    VideoControl.Arrange(new Rect(new System.Windows.Point(currGame.pixelCount++, 0), new System.Windows.Point(2820, 1580)));
                    if (currGame.pixelCount > 0)
                    {
                        currGame.goingLeft = true;
                    }
                }

                // update car position
                currGame.car.X = currGame.pixelCount / 2 + 1000;

            }, this.Dispatcher);

            // handle target hits
            DispatcherTimer hitHandler = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 1), DispatcherPriority.Normal, delegate
            {
                if (currGame.targetAppears)
                {
                    Point calibratedCenter = this.SkeletonPointToScreen(currGame.player.center);

                    double crosshairX = ((int)calibratedCenter.X * (int)this.ActualWidth) / this.sensor.ColorStream.FrameWidth - 300;
                    double crosshairY = ((int)calibratedCenter.Y * (int)this.ActualHeight) / this.sensor.ColorStream.FrameHeight;

                    double crosshairLeft = crosshairX - (crosshair.Width / 2);
                    double crosshairTop = crosshairY - (crosshair.Height / 2);
                    crosshair.Margin = new Thickness(crosshairLeft, crosshairTop, 0, 0);

                    if (currGame.crosshair != null)
                    {
                        PlayCanvas.Children.Remove(currGame.crosshair);
                    }

                    // PlayCanvas.Children.Add(explosion);
                    //Helper.MoveTo(explosion, x, y);
                    // currGame.currExplosion = explosion;
                    PlayCanvas.Children.Add(crosshair);
                    //Panel.SetZIndex(crosshair, 12);
                    currGame.crosshair = crosshair; 

                    if (playerHit(new Point(crosshairX, crosshairY)))
                    {
                        
                        currGame.score += 5000;
                        lblScore.Content = currGame.score;

                        playExplosion();

                        if (currGame.numOfLives == 1)
                        {
                            RemoveTarget();
                            currGame.gameOver = true;
                            playWinSequence();
                        }
                        else
                        {
                            this.HealthBarImage.Source = new BitmapImage(new Uri((@"../../images/" + (currGame.numOfLives - 1).ToString() + "livesleft.png"), UriKind.Relative));
                        }

                        currGame.numOfLives--;
                        RemoveTarget();


                    }
                }

            }, this.Dispatcher);
        }

        private void RemoveTarget()
        {
            this.CombinationImage.Visibility = System.Windows.Visibility.Hidden;

            // init next target
            currGame.currTargetIndex++;
            currGame.targetAppears = false;
            currGame.currTargetSecCounter = 0;
        }



        private void playWinSequence()
        {

            VideoControl.Stop();
            ImageBrush background = new ImageBrush();
            ////spaceship.SetValue(Canvas.ZIndexProperty, 100000000);
            background.ImageSource = new BitmapImage(new Uri(@"../../images/Victory.png", UriKind.Relative));
            VideoControl.Visibility = Visibility.Hidden;
            HealthBarImage.Visibility = Visibility.Hidden;
            background.Stretch = Stretch.Fill;

            PlayCanvas.Children.Remove(currGame.crosshair);
            
            //PlayCanvas.Background = background;//System.Windows.Media.Brushes.Black;
            this.Background = background;
            winningSoundPlayer.Play();
            // and end game
            //throw new NotImplementedException();
        }

        private void playExplosion()
        {
            //currGame.crosshair.Source = this.explosionBitmap;
            currGame.crosshair.Visibility = System.Windows.Visibility.Hidden;

            explosionGif.LoadSmile(new System.Drawing.Bitmap(@"../../images/anim_explode.gif"));
            explosionGif.Visibility = System.Windows.Visibility.Visible;
            explosionGif.IsEnabled = true;
            explosionGif.StartAnimate();
            explosionGif.Margin = currGame.crosshair.Margin;

            PlayCanvas.Children.Add(explosionGif);

            currGame.explosion = true;
            
            soundPlayer.Play();
            soundPlayer.Position = new TimeSpan(0, 0, 0);
        }

        private bool playerHit(Point player)
        {
            if (currGame.gameOver)
            {
                return false;
            }
            bool positioHit = false;

            Point car = currGame.car;

            if ((player.X > (car.X - currGame.CAR_RADIUS)) && (player.X < (car.X + currGame.CAR_RADIUS)) &&
                (player.Y > (car.Y + currGame.CAR_RADIUS)))
            {
                lblScore.Content = "Hit";
                positioHit = true;
            }

            bool weaponHit = (currGame.player.weapon != null) && (currGame.player.weapon.inputText == currGame.currTarget.inputText);

            return (positioHit && weaponHit);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                VideoControl.LoadedBehavior = MediaState.Manual;
                VideoControl.Stop();
                if (null != this.sensor)
                {
                    this.sensor.Stop();
                }

                if ((this._listenThread != null) && (this._listenThread.IsAlive))
                {
                    try
                    {
                        this._server.is_active = false;
                        this._server.listener.Stop();
                    }
                    catch { }
                    this._listenThread.Abort();
                    this._listenThread = null;
                }
            }
            catch { }
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
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        // RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {

                            this.updatePlayerPosition(skel); // debug stuff
                            this.RenderPlayerHands(dc);
                            this.DrawBonesAndJoints(skel, dc);
                            break; // moualem
                        }
                    }
                }

                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        private void DisplaySkeletonPoint(SkeletonPoint sp)
        {
            /* debug stuff
            //lblQrPoints.Content = sp.X + " " + sp.Y + " " + sp.Z;
            lblQrText.Content = sp.X + " " + sp.Y + " " + sp.Z;
            lblScore.Content = sp.X + " " + sp.Y + " " + sp.Z;
            lblStatusBar.Content = sp.X + " " + sp.Y + " " + sp.Z;
            lblQrText.Content = "delta = " + (currGame.player.leftHand.Z - currGame.player.rightHand.Z);
             */
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    this.colorImgSource.WritePixels(
                        new Int32Rect(0, 0, this.colorImgSource.PixelWidth, this.colorImgSource.PixelHeight),
                        this.colorPixels,
                        this.colorImgSource.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void updatePlayerPosition(Skeleton skeleton)
        {
            currGame.player.UpdateHands(skeleton);
        }

        private void RenderPlayerHands(DrawingContext dc)
        {
            dc.DrawEllipse(this.inferredJointBrush, null, this.SkeletonPointToScreen(currGame.player.leftHand), PlayerThickness, PlayerThickness);
            dc.DrawEllipse(this.inferredJointBrush, null, this.SkeletonPointToScreen(currGame.player.rightHand), PlayerThickness, PlayerThickness);
            dc.DrawEllipse(this.playerPointBrush, null, this.SkeletonPointToScreen(currGame.player.center), PlayerThickness, PlayerThickness);

            DisplaySkeletonPoint(currGame.player.center);
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

    }
}
