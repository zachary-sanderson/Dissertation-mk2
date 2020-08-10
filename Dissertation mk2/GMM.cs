using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;

namespace Dissertation_mk2
{
    class GMM
    {
        public double[][] Means { get; }

        public double[] Proportions { get; }

        public GMM(List<List<double>> points) 
        {
            Accord.Math.Random.Generator.Seed = 0;

            var samples = points.Select(a => a.ToArray()).ToArray();

            // Create a multivariate Gaussian for 2 dimensions
            var normal = new MultivariateNormalDistribution(2);

            // Specify a regularization constant in the fitting options
            NormalOptions options = new NormalOptions() { Regularization = double.Epsilon };

            // Fit the distribution to the data
            normal.Fit(samples, options);

            // Create a new Gaussian Mixture Model with 2 components
            GaussianMixtureModel gmm = new GaussianMixtureModel(8);

            gmm.Options = options;

            // Estimate the Gaussian Mixture
            var clusters = gmm.Learn(samples);

            Means = clusters.Means;

            Proportions = clusters.Proportions;
        }

        //Compare another GMMs cluster to this one
        public double Compare(GMM otherGmm)
        {
            double[][] otherMeans = otherGmm.Means;
            var (closestGaussian , closestDists) = CompareGaussians(otherMeans);

            double[] otherProportions = otherGmm.Proportions;

            double diff = 0;

            for (int i = 0; i < closestGaussian.Length; i++)
            {
                var otherProportion = otherProportions[closestGaussian[i]];
                var proportionalDiff = Math.Abs(Proportions[i] - otherProportion);
                diff += closestDists[i] = (proportionalDiff * closestDists[i]);
            }

            return diff;
        }

        //Compare the means of another GMM and return the index for each closest mean in the other GMM
        private (int[], double[]) CompareGaussians(double[][] otherMeans)
        {
            int[] closestGaussians = new int[Means.Length];
            double[] closestDists = new double[Means.Length];

            for (int i = 0; i < Means.Length; i++)
            {
                int closestIndex = 0;
                double closestDistance = 1000;
                for (int j = 0; j < otherMeans.Length; j++)
                {
                    var dist = CompareMeans(Means[i], otherMeans[j]);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestIndex = j;
                    }

                    closestGaussians[i] = closestIndex;
                    closestDists[i] = closestDistance;
                }
            }

            return (closestGaussians, closestDists);
        }

        //Compare dist between the means of 2 GMM's
        private double CompareMeans(double[] mean, double[] otherMean)
        {
            double xDiff, yDiff;
            xDiff = Math.Abs(mean[0] - otherMean[0]);
            yDiff = Math.Abs(mean[1] - mean[1]);
            return Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
        }
    }
}
