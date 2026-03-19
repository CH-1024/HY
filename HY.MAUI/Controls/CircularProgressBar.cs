using System;
using System.Collections.Generic;
using System.Text;

namespace HY.MAUI.Controls
{
    public class CircularProgressBar : GraphicsView, IDrawable
    {
        public CircularProgressBar()
        {
            Drawable = this;
        }

        #region BindableProperty

        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(
                nameof(Progress),
                typeof(double),
                typeof(CircularProgressBar),
                0d,
                propertyChanged: (b, o, n) =>
                {
                    ((CircularProgressBar)b).Invalidate();
                });

        /// <summary>
        /// 0~1
        /// </summary>
        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly BindableProperty ShowTrackProperty =
            BindableProperty.Create(
                nameof(ShowTrack),
                typeof(bool),
                typeof(CircularProgressBar),
                true,
                propertyChanged: (b, o, n) =>
                {
                    ((CircularProgressBar)b).Invalidate();
                });

        /// <summary>
        /// 显示背景轨迹
        /// </summary>
        public bool ShowTrack
        {
            get => (bool)GetValue(ShowTrackProperty);
            set => SetValue(ShowTrackProperty, value);
        }

        public static readonly BindableProperty ProgressColorProperty =
            BindableProperty.Create(
                nameof(ProgressColor),
                typeof(Color),
                typeof(CircularProgressBar),
                Colors.DodgerBlue,
                propertyChanged: (b, o, n) =>
                {
                    ((CircularProgressBar)b).Invalidate();
                });

        public Color ProgressColor
        {
            get => (Color)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public static readonly BindableProperty TrackColorProperty =
            BindableProperty.Create(
                nameof(TrackColor),
                typeof(Color),
                typeof(CircularProgressBar),
                Colors.LightGray,
                propertyChanged: (b, o, n) =>
                {
                    ((CircularProgressBar)b).Invalidate();
                });

        public Color TrackColor
        {
            get => (Color)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }

        public static readonly BindableProperty StrokeThicknessProperty =
            BindableProperty.Create(
                nameof(StrokeThickness),
                typeof(float),
                typeof(CircularProgressBar),
                8f,
                propertyChanged: (b, o, n) =>
                {
                    ((CircularProgressBar)b).Invalidate();
                });

        public float StrokeThickness
        {
            get => (float)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        #endregion

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float stroke = StrokeThickness;

            float size = Math.Min(dirtyRect.Width, dirtyRect.Height) - stroke;
            float x = (dirtyRect.Width - size) / 2;
            float y = (dirtyRect.Height - size) / 2;

            var rect = new RectF(x, y, size, size);

            float cx = rect.Center.X;
            float cy = rect.Center.Y;
            float radius = rect.Width / 2;

            canvas.StrokeSize = stroke;

            // ===== 背景圆 =====
            if (ShowTrack)
            {
                canvas.StrokeColor = TrackColor;
                canvas.StrokeLineCap = LineCap.Butt;

                var trackPath = new PathF();
                trackPath.AppendCircle(cx, cy, radius);

                canvas.DrawPath(trackPath);
            }

            // ===== 进度 =====
            double progress = Math.Clamp(Progress, 0, 1);

            if (progress <= 0)
                return;

            canvas.StrokeColor = ProgressColor;
            canvas.StrokeLineCap = LineCap.Round;

            float startAngle = -90f;
            float sweepAngle = (float)(360 * progress);

            var progressPath = new PathF();

            progressPath.AddArc(
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                startAngle,
                startAngle + sweepAngle,
                false);

            canvas.DrawPath(progressPath);
        }
    }
}
