﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PewPew
{
    static class Helper
    {

        public static void MoveTo(this Image target, double newX, double newY)
        {
            Vector offset = VisualTreeHelper.GetOffset(target);
            var top = offset.Y;
            var left = offset.X;
            TranslateTransform trans = new TranslateTransform();
            target.RenderTransform = trans;
            DoubleAnimation anim1 = new DoubleAnimation(0, newY - top, TimeSpan.FromSeconds(10));
            DoubleAnimation anim2 = new DoubleAnimation(0, newX - left, TimeSpan.FromSeconds(10));
            trans.BeginAnimation(TranslateTransform.YProperty, anim1);
            trans.BeginAnimation(TranslateTransform.XProperty, anim2);
        }
    }
}
