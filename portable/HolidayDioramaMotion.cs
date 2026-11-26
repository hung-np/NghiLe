using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace NghiLe
{
    internal static partial class HolidayDiorama
    {
        private sealed class Rig
        {
            internal readonly Model3DGroup Model = new Model3DGroup();
            private readonly List<Action<double>> motion = new List<Action<double>>();

            internal void Animate(Action<double> pose) { motion.Add(pose); }

            internal void Pose(double phase)
            {
                foreach (var pose in motion) pose(phase);
            }
        }

        private const int FramesPerCycle = 96;
        private const int Stride = SceneWidth * 4;
        private const int PixelBytes = Stride * SceneHeight;
        private const int ArchiveEntries = FramesPerCycle + 1;
        private const string ArchivePrefix = "NghiLe.Diorama.";

        // Only the visible scene owns animation data and a mutable WPF image.
        internal sealed class Playback
        {
            private static readonly Int32Rect Bounds = new Int32Rect(0, 0, SceneWidth, SceneHeight);
            private Diorama current;
            private byte[][] frames;
            private byte[] pixels;
            private WriteableBitmap display;
            private int displayed = -1;

            internal BitmapSource Frame(HolidayTheme theme, bool apricot, double phase)
            {
                var next = SceneFor(theme, apricot);
                if (current != next)
                {
                    Reset();
                    current = next;
                    frames = next.GetAnimationFrames();
                    if (frames != null)
                    {
                        pixels = new byte[PixelBytes];
                        display = new WriteableBitmap(SceneWidth, SceneHeight, 96, 96, PixelFormats.Pbgra32, null);
                    }
                }

                if (frames == null) return null;
                int frame = Math.Min(FramesPerCycle - 1, (int)(phase * FramesPerCycle));
                if (displayed == frame) return display;

                if (!DecodeFrame(frames[frame], pixels))
                {
                    Reset();
                    current = next;
                    return null;
                }

                display.WritePixels(Bounds, pixels, Stride, 0);
                displayed = frame;
                return display;
            }

            internal void Reset()
            {
                current = null;
                frames = null;
                pixels = null;
                display = null;
                displayed = -1;
            }
        }

        private static BitmapSource LoadStill(Diorama scene)
        {
            var frames = ReadFrames(scene, false);
            if (frames == null) return null;

            var pixels = new byte[PixelBytes];
            if (!DecodeFrame(frames[0], pixels)) return null;
            var image = BitmapSource.Create(SceneWidth, SceneHeight, 96, 96, PixelFormats.Pbgra32, null, pixels, Stride);
            image.Freeze();
            return image;
        }

        private static byte[][] ReadFrames(Diorama scene, bool animated)
        {
            using (var asset = typeof(HolidayDiorama).Assembly.GetManifestResourceStream(ArchivePrefix + scene.AssetName))
            {
                if (asset == null || asset.Length < ArchiveEntries * sizeof(int)) return null;
                using (var reader = new BinaryReader(asset))
                {
                    var lengths = new int[ArchiveEntries];
                    long bytes = 0;
                    for (int index = 0; index < lengths.Length; index++)
                    {
                        int length = reader.ReadInt32();
                        if (length <= 0 || length > PixelBytes * 2) return null;
                        lengths[index] = length;
                        bytes += length;
                    }
                    if (asset.Length - asset.Position != bytes) return null;
                    if (!animated) return new[] { reader.ReadBytes(lengths[0]) };

                    asset.Position += lengths[0];
                    var frames = new byte[FramesPerCycle][];
                    for (int frame = 0; frame < frames.Length; frame++)
                        frames[frame] = reader.ReadBytes(lengths[frame + 1]);
                    return frames;
                }
            }
        }

        private static bool DecodeFrame(byte[] compressed, byte[] pixels)
        {
            try
            {
                using (var source = new MemoryStream(compressed, false))
                using (var stream = new DeflateStream(source, CompressionMode.Decompress))
                {
                    int offset = 0;
                    while (offset < pixels.Length)
                    {
                        int count = stream.Read(pixels, offset, pixels.Length - offset);
                        if (count == 0) return false;
                        offset += count;
                    }
                }
                return true;
            }
            catch (InvalidDataException) { return false; }
        }

        internal static void BakeArchives(string folder)
        {
            foreach (var scene in Scenes.Values) BakeArchive(folder, scene);
            BakeArchive(folder, Apricot);
        }

        private static void BakeArchive(string folder, Diorama scene)
        {
            var pixels = new byte[PixelBytes];
            var frames = new byte[ArchiveEntries][];
            frames[0] = BakeFrame(new Shot(scene, false), 0, pixels, scene.AssetName);
            var shot = new Shot(scene, true);
            for (int frame = 0; frame < FramesPerCycle; frame++)
                frames[frame + 1] = BakeFrame(shot, frame / (double)FramesPerCycle, pixels, scene.AssetName);

            using (var writer = new BinaryWriter(File.Create(Path.Combine(folder, scene.AssetName + ".diorama"))))
            {
                foreach (var frame in frames) writer.Write(frame.Length);
                foreach (var frame in frames) writer.Write(frame);
            }
        }

        private static byte[] BakeFrame(Shot shot, double phase, byte[] pixels, string scene)
        {
            if (!shot.CopyFrame(phase, pixels)) throw new InvalidOperationException("Cannot render " + scene);
            using (var compressed = new MemoryStream())
            {
                using (var stream = new DeflateStream(compressed, CompressionLevel.Optimal, true))
                    stream.Write(pixels, 0, pixels.Length);
                return compressed.ToArray();
            }
        }

        private sealed class Shot
        {
            private readonly Rig rig = new Rig();
            private readonly Viewport3D viewport;
            private readonly OrthographicCamera camera;
            private readonly DirectionalLight key;
            private readonly bool moving;
            private readonly RenderTargetBitmap bitmap = new RenderTargetBitmap(
                SceneWidth, SceneHeight, 96, 96, PixelFormats.Pbgra32);
            private bool checkedArtwork;

            internal Shot(Diorama source, bool moving)
            {
                this.moving = moving;

                var scene = rig.Model;
                scene.Children.Add(new AmbientLight(Color.FromRgb(112, 101, 111)));
                key = new DirectionalLight(Color.FromRgb(242, 225, 203), new Vector3D(.56, -1, -.52));
                scene.Children.Add(key);
                scene.Children.Add(new DirectionalLight(Color.FromRgb(99, 93, 118), new Vector3D(-.7, -.52, .54)));
                BuildTray(scene, source.BaseColor, source.Surface);
                source.Model(rig);

                camera = new OrthographicCamera
                {
                    UpDirection = new Vector3D(0, 1, 0),
                    Width = 5.35
                };
                AimCamera(2.4);
                viewport = new Viewport3D { Camera = camera, IsHitTestVisible = false };
                viewport.Children.Add(new ModelVisual3D { Content = scene });
                var size = new Size(SceneWidth, SceneHeight);
                viewport.Measure(size);
                viewport.Arrange(new Rect(size));
            }

            private void AimCamera(double x)
            {
                var eye = new Point3D(x, 3.35, 10);
                camera.Position = eye;
                camera.LookDirection = new Point3D(.02, 1.06, 0) - eye;
            }



            internal bool CopyFrame(double phase, byte[] destination)
            {
                if (!RenderFrame(phase)) return false;
                bitmap.CopyPixels(destination, SceneWidth * 4, 0);
                return true;
            }

            private bool RenderFrame(double phase)
            {
                if (moving)
                {
                    double sway = Math.Sin(phase * 2 * Math.PI);
                    AimCamera(2.4 + .68 * sway);
                    key.Direction = new Vector3D(.56 + .16 * sway, -1, -.52);
                    rig.Pose(phase);
                }

                try
                {
                    if (checkedArtwork) bitmap.Clear();
                    bitmap.Render(viewport);
                    if (!checkedArtwork)
                    {
                        if (!HasArtwork(bitmap)) return false;
                        checkedArtwork = true;
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // A visual failure can fall back to the still or the original 2D artwork.
                    return false;
                }
            }

            private static bool HasArtwork(BitmapSource bitmap)
            {
                var row = new byte[SceneWidth * 4];
                bitmap.CopyPixels(new Int32Rect(0, SceneHeight / 2, SceneWidth, 1), row, row.Length, 0);
                for (int index = 3; index < row.Length; index += 4)
                    if (row[index] != 0) return true;
                return false;
            }
        }

        private static AxisAngleRotation3D Pivot(Model3DGroup group, Vector3D axis, Point3D origin)
        {
            var rotation = new AxisAngleRotation3D(axis, 0);
            group.Transform = new RotateTransform3D(rotation, origin);
            return rotation;
        }

        private static void ContactShadow(Model3DGroup scene, Point3D center, double width, double depth)
        {
            var mesh = new MeshGeometry3D();
            double x = width / 2, z = depth / 2;
            Quad(mesh, center + new Vector3D(-x, 0, z), center + new Vector3D(x, 0, z),
                center + new Vector3D(x, 0, -z), center + new Vector3D(-x, 0, -z));
            var shadow = new DiffuseMaterial(new RadialGradientBrush(
                Color.FromArgb(173, 16, 9, 14), Color.FromArgb(0, 16, 9, 14))
            {
                GradientOrigin = new Point(.46, .45)
            });
            scene.Children.Add(new GeometryModel3D(mesh, shadow));
        }
    }
}
