﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PewPew.Game
{
    class MyGame
    {
        public bool targetAppears = false;
        public int currTargetSecCounter = 0;
        public int currTargetIndex = 0;
        public bool goingLeft = false;
        public int pixelCount = 0;
        public int numOfLives = 3;
        public TargetType currTarget = null;
        public Player player;
        public Image crosshair = null;
        
        public int score = 0;
        public bool gameOver = false;

        public Boolean explosion = false;

        public int CAR_RADIUS = 200;
        public Point car = new Point() { X = 0, Y = 0 };

        public MyGame()
        {
            player = new Player();
        }
    }
}
