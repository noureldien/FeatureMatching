using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
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

namespace FeatureMatching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Sift()
        {
            Mat src1 = new Mat("image1.png", LoadMode.GrayScale);
            Mat src2 = new Mat("image2.png", LoadMode.GrayScale);

            // Detect the keypoints and generate their descriptors using SIFT
            SIFT sift = new SIFT();

            KeyPoint[] keypoints1, keypoints2;
            MatOfFloat descriptors1 = new MatOfFloat();
            MatOfFloat descriptors2 = new MatOfFloat();
            sift.Run(src1, null, out keypoints1, descriptors1);
            sift.Run(src2, null, out keypoints2, descriptors2);

            // Matching descriptor vectors with a brute force matcher
            BFMatcher matcher = new BFMatcher(NormType.L2, false);
            DMatch[] matches = matcher.Match(descriptors1, descriptors2);

            // Draw matches
            Mat view = new Mat();
            Cv2.DrawMatches(src1, keypoints1, src2, keypoints2, matches, view);

            using (new OpenCvSharp.CPlusPlus.Window("SIFT matching", WindowMode.AutoSize, view))
            {
                Cv2.WaitKey();
            }
        }

        private void Surf()
        {
            Mat src1 = new Mat("image1.png", LoadMode.GrayScale);
            Mat src2 = new Mat("image2.png", LoadMode.GrayScale);

            // Detect the keypoints and generate their descriptors using SURF
            SURF surf = new SURF(500, 4, 2, true);

            KeyPoint[] keypoints1, keypoints2;
            MatOfFloat descriptors1 = new MatOfFloat();
            MatOfFloat descriptors2 = new MatOfFloat();
            surf.Run(src1, null, out keypoints1, descriptors1);
            surf.Run(src2, null, out keypoints2, descriptors2);

            // Matching descriptor vectors with a brute force matcher
            BFMatcher matcher = new BFMatcher(NormType.L2, false);
            DMatch[] matches = matcher.Match(descriptors1, descriptors2);

            // Draw matches
            Mat view = new Mat();
            Cv2.DrawMatches(src1, keypoints1, src2, keypoints2, matches, view);

            using (new OpenCvSharp.CPlusPlus.Window("SURF matching", WindowMode.AutoSize, view))
            {
                Cv2.WaitKey();
            }
        }
    }
}
