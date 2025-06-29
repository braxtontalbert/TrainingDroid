using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TrainingDroid
{
    public class DroidOptions : ThunderScript
    {
        public static DroidOptions local;
        public List<TrainingDroid> droids = new List<TrainingDroid>();
        public static float[] diffcultyLevels = new float[6];
        public static int difficultyIndex = 0;
        public static string currentColor = "Yellow";
        
        [ModOption("Blaster Bolt Color", valueSourceName = nameof(color), defaultValueIndex = 2,
            category = "Training Droid")]
        [ModOptionOrder(1)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        [ModOptionTooltip("Changes color for training droid blaster")]
        public static void Color(string color)
        {
            currentColor = color;
        }
        
        [ModOption("Droid Difficulty", valueSourceName = nameof(droidDifficulty), defaultValueIndex = 1,
            category = "Training Droid")]
        [ModOptionOrder(2)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        [ModOptionTooltip("Difficulty Level for the Training Droid")]
        public static void DroidLevel(int difficulty)
        {
            difficultyIndex = Mathf.Clamp(difficulty, 0, droidDifficulty.Length);
            
            foreach (var droid in local.droids)
            {
                droid.ResetCoroutines();
            }
        }
        
        public static ModOptionInt[] droidDifficulty = new ModOptionInt[]
        {
            new ModOptionInt("Story Mode", 0),
            new ModOptionInt("Padawan", 1),
            new ModOptionInt("Jedi Knight", 2),
            new ModOptionInt("Jedi Master", 3),
            new ModOptionInt("Jedi Grand Master", 4),
            new ModOptionInt("Will of the Force", 5)
        };
        
        public static ModOptionString[] color = new ModOptionString[]
        {
            new ModOptionString("Red", "Red"),
            new ModOptionString("Blue", "Blue"),
            new ModOptionString("Yellow", "Yellow"),
            new ModOptionString("Orange", "Orange"),
            new ModOptionString("Training", "Training"),
            new ModOptionString("Cyan", "Cyan"),
            new ModOptionString("Green", "Green")
        };

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (local is null)
            {
                local = this;
            }   
            
            diffcultyLevels[0] = 1f;
            diffcultyLevels[1] = 0.7f;
            diffcultyLevels[2] = 0.5f;
            diffcultyLevels[3] = 0.3f;
            diffcultyLevels[4] = 0.1f;
            diffcultyLevels[5] = 0.00001f;
        }
    }
}