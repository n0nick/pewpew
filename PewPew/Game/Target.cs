using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PewPew.Game
{
    public class TargetType
    {
        public string inputText;
        public System.Windows.Media.SolidColorBrush color;
        public string fileName;
    }

    class Target
    {
        public enum TargetName { Action1, Action2, Action3, Action4 };
        public static Dictionary<TargetName, TargetType> EnemyTypes = new Dictionary<TargetName, TargetType>()
        { 
            { TargetName.Action1, new TargetType { inputText = "ice,water,wind", color = System.Windows.Media.Brushes.ForestGreen, fileName = "comb1.png" } },
            { TargetName.Action2, new TargetType { inputText = "fire,magic,stone", color = System.Windows.Media.Brushes.Yellow, fileName = "comb2.png"  } },
            { TargetName.Action3, new TargetType { inputText = "ice,magic,wind", color = System.Windows.Media.Brushes.Purple, fileName = "comb3.png"  } },
            { TargetName.Action4, new TargetType { inputText = "fire,ice,wind", color = System.Windows.Media.Brushes.Teal, fileName = "comb4.png" } }
        };

        public static TargetType EnemyTypeByInputText(String inputText)
        {
            return EnemyTypes.FirstOrDefault(type => type.Value.inputText == inputText).Value;
        }
    }
}

