using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows.Controls;
using OpenCvSharp;
using OpenCvSharp.Blob;
using System.Runtime.InteropServices;
using OpenCvSharp.CPlusPlus;

namespace FeatureMatching
{
    /// <summary>
    /// Responsible of Image Processing.
    /// </summary>
    public class Tracker
    {
        #region Private Enums

        /// <summary>
        /// Algorithm used to extract features.
        /// </summary>
        public enum FeatureType
        {
            Sift = 0,
            Surf = 1,
        }

        /// <summary>
        /// Matcher used to match features.
        /// </summary>
        public enum MatcherType
        {
            FlannBased = 0,
            BruteForce = 1,
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Horizontally invert the captured frame from the camera.
        /// </summary>
        public bool InvertHorizontal { get; set; }
        /// <summary>
        /// Vertically invert the captured frame from the camera.
        /// </summary>
        public bool InvertVertical { get; set; }
        /// <summary>
        /// Use grayscale images as input or not.
        /// </summary>
        public bool IsGrayScale { get; set; }
        /// <summary>
        /// Use salt and pepper noise to the images.
        /// </summary>
        public bool IsNoise { get; set; }
        /// <summary>
        /// Use good matching or all matching.
        /// </summary>
        public bool IsGoodMatching { get; set; }
        /// <summary>
        /// Use homography check or not.
        /// </summary>
        public bool IsHomography { get; set; }
        /// <summary>
        /// Value of Guassian smooth.
        /// </summary>
        public int GaussianSmooth { get; set; }
        /// <summary>
        /// Value of the threshold used in good matching.
        /// </summary>
        public double GoodMatchingThreshold { get; set; }
        /// <summary>
        /// Type of algorithm used to extract features.
        /// </summary>
        public FeatureType FeatureExtractor { get; set; }
        /// <summary>
        /// Type of algorithm used to match features.
        /// </summary>
        public MatcherType FeatureMatcher { get; set; }
        /// <summary>
        /// Set interval time of the main timer.
        /// </summary>
        public int TimerIntervalTime
        {
            set
            {
                timerIntervalTime = value;
                mainTimer.Interval = timerIntervalTime;
            }

            get
            {
                return timerIntervalTime;
            }
        }

        #endregion

        #region Private Variables

        private readonly int deviceID = 0;
        private readonly int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
        private readonly int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;

        private int timerIntervalTime = 30;
        private int counter = 0;
        private Label labelFrameCounter;

        private bool timerInProgress = false;
        private System.Windows.Forms.Timer fpsTimer;
        private System.Windows.Forms.Timer mainTimer;

        private CvCapture capture;
        private IplImage frame1;
        private IplImage frame2;
        private IplImage grayFrame1;
        private IplImage grayFrame2;
        private IplImage transformedFrame;
        private CvWindow window1;
        private CvWindow window2;
        private CvWindow window3;
        private CvSize size;
        private SIFT sift;
        private SURF surf;
        private BFMatcher bruteForceMatcher;
        private FlannBasedMatcher flannBasedMatcher;

        #endregion

        #region Constructor

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="labelFrameCounter"></param>
        public Tracker(Label labelFrameCounter)
        {
            this.labelFrameCounter = labelFrameCounter;
            Initialize();
            InitializeCamera();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// used to dispose any object created from this class
        /// </summary>
        public void Dispose()
        {
            if (timerInProgress)
            {
                mainTimer.Stop();
                fpsTimer.Stop();
            }

            if (mainTimer != null)
            {
                mainTimer.Dispose();
                mainTimer = null;
            }

            if (fpsTimer != null)
            {
                fpsTimer.Dispose();
                fpsTimer = null;
            }

            if (window1 != null)
            {
                window1.Close();
                window1.Dispose();
                window1 = null;
            }

            if (window2 != null)
            {
                window2.Close();
                window2.Dispose();
                window2 = null;
            }

            if (window3 != null)
            {
                window3.Close();
                window3.Dispose();
                window3 = null;
            }

            if (capture != null)
            {
                capture.Dispose();
                capture = null;
            }
        }

        /// <summary>
        /// Start mainThread, that starts tracking
        /// </summary>
        public void StartProcessing()
        {
            mainTimer.Start();
            fpsTimer.Start();
            timerInProgress = true;
        }

        /// <summary>
        /// Stop mainThread, that stops tracking
        /// </summary>
        public void StopProcessing()
        {
            mainTimer.Stop();
            fpsTimer.Stop();
            timerInProgress = false;
        }

        /// <summary>
        /// Take snapshot and save it as camera 2.
        /// </summary>
        public void TakeSnapshot()
        {
            frame2 = capture.QueryFrame().Clone();
            isReady = true;
        }

        bool isReady = false;

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize Camera, timer and some objects.
        /// </summary>
        private void Initialize()
        {
            // initialize mainTimer
            mainTimer = new System.Windows.Forms.Timer();
            mainTimer.Interval = timerIntervalTime;
            mainTimer.Tick += ProcessFrame;

            // initialize timer used to count frames per seconds of the camera
            fpsTimer = new System.Windows.Forms.Timer();
            fpsTimer.Interval = 1000;
            fpsTimer.Tick += new EventHandler((object obj, EventArgs eventArgs) =>
            {
                labelFrameCounter.Content = counter.ToString();
                counter = 0;
            });

            GoodMatchingThreshold = 2;
            IsGoodMatching = true;
            IsHomography = true;
        }

        /// <summary>
        /// Initialize camera input, frame window and other image objects required.
        /// This is done after getting the settings of the tracker object of this class.
        /// </summary>
        private void InitializeCamera()
        {
            // Intialize camera
            try
            {
                //videoInput = new VideoInput();
                capture = new CvCapture(CaptureDevice.Any, deviceID);
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show("Failed to initialize the camera, the program will be closed." +
                    "\n\nThis is the internal error:\n" + exception.Message, "Notify", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }

            // small frame to decrease computational complexity
            size = new CvSize(320, 240);

            capture.SetCaptureProperty(CaptureProperty.FrameHeight, size.Height);
            capture.SetCaptureProperty(CaptureProperty.FrameWidth, size.Width);
            capture.SetCaptureProperty(CaptureProperty.FrameCount, 15);

            frame1 = new IplImage(size, BitDepth.U8, 3);
            frame2 = new IplImage(size, BitDepth.U8, 3);
            grayFrame1 = new IplImage(size, BitDepth.U8, 1);
            grayFrame2 = new IplImage(size, BitDepth.U8, 1);
            transformedFrame = new IplImage(size, BitDepth.U8, 1);
            sift = new SIFT();
            surf = new SURF(500, 4, 2, true);
            bruteForceMatcher = new BFMatcher(NormType.L2, false);
            flannBasedMatcher = new FlannBasedMatcher();

            // windows to view what's going on
            window1 = new CvWindow("Camera 1", WindowMode.KeepRatio);
            window1.Resize(size.Width, size.Height);
            window1.Move(screenWidth - 17 - 2 * size.Width, 20);

            window2 = new CvWindow("Camera 2", WindowMode.KeepRatio);
            window2.Resize(size.Width, size.Height);
            window2.Move(screenWidth - 20 - 1 * size.Width, 20);

            window3 = new CvWindow("Result", WindowMode.KeepRatio);
            window3.Resize(size.Width * 2, size.Height);
            window3.Move(screenWidth - 20 - 2 * size.Width, 20 + size.Height);
        }


        /// <summary>
        /// Image Processing. It is done using OpenCVSharp Library.
        /// </summary>
        private void ProcessFrame(object sender, EventArgs e)
        {
            // increment counter
            counter++;
            frame1 = capture.QueryFrame();

            // show image on the separate window
            window1.Image = frame1;
            window2.Image = frame2;

            // apply some variations to the image (brightness, salt-and-pepper, ...)
            if (isReady)
            {
                IplImage image1;
                IplImage image2;

                // check if to use noise/gray-scale or not
                if (IsNoise || IsGrayScale)
                {
                    // convert to grayscale image
                    Cv.CvtColor(frame1, grayFrame1, ColorConversion.BgrToGray);
                    Cv.CvtColor(frame2, grayFrame2, ColorConversion.BgrToGray);
                    image1 = grayFrame1;
                    image2 = grayFrame2;

                    if (IsNoise)
                    {
                        image1 = Noise(image1);
                    }
                }
                else
                {
                    image1 = frame1;
                    image2 = frame2;
                }

                // check if to use gaussian smooth
                if (GaussianSmooth > 0)
                {
                    int gaussianValue = (GaussianSmooth % 2 == 0) ? GaussianSmooth - 1 : GaussianSmooth;
                    Cv.Smooth(image1, image1, SmoothType.Gaussian, gaussianValue);
                }

                // check to flip the image or not
                if (InvertHorizontal && InvertVertical)
                {
                    Cv.Flip(image1, image1, FlipMode.XY);
                }
                else
                {
                    if (InvertHorizontal)
                    {
                        Cv.Flip(image1, image1, FlipMode.Y);
                    }
                    if (InvertVertical)
                    {
                        Cv.Flip(image1, image1, FlipMode.X);
                    }
                }

                // apply the matching
                transformedFrame = Matching(image1, image2, FeatureExtractor);

                window3.Image = transformedFrame;
            }
        }

        /// <summary>
        /// Feature extraction and matching on the given 2 images.
        /// The result is the images concatenated together with features,
        /// matching and homography drawn on it.
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <param name="featureType"></param>
        /// <returns></returns>
        private IplImage Matching(IplImage image1, IplImage image2, FeatureType featureType)
        {
            Mat src1 = new Mat(image1);
            Mat src2 = new Mat(image2);

            KeyPoint[] keypoints1;
            KeyPoint[] keypoints2;
            MatOfFloat descriptors1 = new MatOfFloat();
            MatOfFloat descriptors2 = new MatOfFloat();

            // extract features with different feature-extration methods
            switch (featureType)
            {
                case FeatureType.Sift:
                    sift.Run(src1, null, out keypoints1, descriptors1);
                    sift.Run(src2, null, out keypoints2, descriptors2);
                    break;
                case FeatureType.Surf:
                    surf.Run(src1, null, out keypoints1, descriptors1);
                    surf.Run(src2, null, out keypoints2, descriptors2);
                    break;
                default:
                    throw new NotSupportedException("Sorry, missing feature type.");
            }

            // matching descriptor vectors with a brute force matcher
            DMatch[] matches;
            switch (FeatureMatcher)
            {
                case MatcherType.BruteForce:
                    matches = bruteForceMatcher.Match(descriptors1, descriptors2);
                    break;
                case MatcherType.FlannBased:
                    matches = flannBasedMatcher.Match(descriptors1, descriptors2);
                    break;
                default:
                    throw new NotSupportedException("Sorry, missing matcher type.");
            }

            // get only "good" matches, only good matches will be drawn
            List<DMatch> goodMatches;

            // // check to get only good matches or all matches
            if (IsGoodMatching)
            {
                // quick calculation of max and min distances between keypoints
                IEnumerable<float> distances = matches.Select(i => i.Distance);
                double maxDistance = 0;
                double minDistance = 100;
                double newMinDistance = distances.Min();
                double newMaxDistance = distances.Max();
                minDistance = (newMinDistance < minDistance) ? newMinDistance : minDistance;
                maxDistance = (newMaxDistance > maxDistance) ? newMaxDistance : maxDistance;

                goodMatches = matches.Where(i => i.Distance <= GoodMatchingThreshold * minDistance).ToList();
            }
            else
            {
                goodMatches = matches.ToList();
            }            

            // draw matches
            Mat view = new Mat();
            Cv2.DrawMatches(src1, keypoints1, src2, keypoints2, goodMatches, view);

            // homography need at least 4 points or more
            if (IsHomography && goodMatches.Count > 4)
            {
                // get good keypoints (localize the object)
                List<Point2d> goodKeypoints1 = new List<Point2d>();
                List<Point2d> goodKeypoints2 = new List<Point2d>();
                Point2f pt;

                // get the keypoints from the good matches
                for (int i = 0; i < goodMatches.Count; i++)
                {
                    pt = keypoints1[goodMatches[i].QueryIdx].Pt;
                    goodKeypoints1.Add(new Point2d(pt.X, pt.Y));

                    pt = keypoints2[goodMatches[i].TrainIdx].Pt;
                    goodKeypoints2.Add(new Point2d(pt.X, pt.Y));
                }

                // find the homography
                Mat homography = Cv2.FindHomography(goodKeypoints2, goodKeypoints1, HomographyMethod.Ransac);

                // get the corners from image1
                InputArray corners1 = InputArray.Create(new Point2f[]
                {
                    new Point2f(0,0),
                    new Point2f(src1.Cols,0),
                    new Point2f(src1.Cols,src1.Rows),
                    new Point2f(0,src1.Rows),
                }.ToList());

                OutputArray corners2 = OutputArray.Create(new Point2f[]
                {
                    new Point2f(0,0),
                    new Point2f(0,0),
                    new Point2f(0,0),
                    new Point2f(0,0),
                }.ToList());

                InputArray perspectiveMatrix = InputArray.Create(homography);
                Cv2.PerspectiveTransform(corners1, corners2, perspectiveMatrix);

                Mat corners2Matrix = corners2.GetMat();
                Point2f point1 = corners2Matrix.At<Point2f>(0, 0);
                Point2f point2 = corners2Matrix.At<Point2f>(1, 0);
                Point2f point3 = corners2Matrix.At<Point2f>(2, 0);
                Point2f point4 = corners2Matrix.At<Point2f>(3, 0);
                Scalar color = new Scalar(0, 200, 253);

                // draw lines between the corners
                Cv2.Line(view, point1, point2, color, 4);
                Cv2.Line(view, point2, point3, color, 4);
                Cv2.Line(view, point3, point4, color, 4);
                Cv2.Line(view, point4, point1, color, 4);
            }

            IplImage result = view.ToIplImage();
            return result;
        }

        /// <summary>
        /// Add salt-and-pepper noise to the given image.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private IplImage Noise(IplImage source)
        {
            IplImage result = source.Clone();
            Mat noise = new Mat(source.Height, source.Width, MatType.CV_32F);
            InputOutputArray noiseArray = (InputOutputArray)noise;
            Cv2.Randu(noiseArray, (Scalar)0, (Scalar)255);
            noise = noiseArray.GetMat();

            int bound = 5;

            int upperBound = 255 - bound;
            int lowerBound = 0 + bound;
            float noiseValue;

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    noiseValue = noise.At<float>(y, x);

                    if (noiseValue >= upperBound)
                    {
                        result[y, x] = new CvScalar(255);
                    }
                    else if (noiseValue <= lowerBound)
                    {
                        result[y, x] = new CvScalar(0);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}