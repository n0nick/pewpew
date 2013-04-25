using Microsoft.Kinect;
using PewPew.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PewPew.Game
{
    public class Player
    {
        public SkeletonPoint leftHand;
        public SkeletonPoint rightHand;
        public SkeletonPoint center;
        public ControllerDirection _contollerDirection;

        public TargetType weapon;

        // private Point screenPoint;

        public void UpdateHands(Skeleton skeleton)
        {
            this.leftHand  = skeleton.Joints[JointType.HandLeft].Position;
            this.rightHand = skeleton.Joints[JointType.HandRight].Position;

            this.center = new SkeletonPoint() {
                X = (this.leftHand.X + this.rightHand.X) / 2,
                Y = (this.leftHand.Y + this.rightHand.Y) / 2,
                Z = (this.leftHand.Z + this.rightHand.Z) / 2
            };
        }

        public void UpdateWeapon(TargetType weapon)
        {
            this.weapon = weapon;
        }

        public void UpdateWeapon(String inputText)
        {
            this.UpdateWeapon(Target.EnemyTypeByInputText(inputText));
        }
    }
}
