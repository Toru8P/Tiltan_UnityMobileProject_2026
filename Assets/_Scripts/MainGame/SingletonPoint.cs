using System;
using _Scripts.MainGame.Difficulty;
using UnityEngine;

namespace _Scripts.MainGame
{
    public class SingletonPoint : MonoBehaviour
    {
        public static SingletonPoint Instance { get; private set; }

        public SingletonPoint()
        {
            if (Instance)
            {
                throw new Exception("SingletonPoint instance already exists! There should only be one in the scene.");
            }
            else
            {
                Instance = this;
            }
        }

        [SerializeField] private GeneralDifficultyManager difficultyManager;
        
        public GeneralDifficultyManager DifficultyManager {
            get
            {
              return difficultyManager;  
            }
            private set
            {
                if (difficultyManager)
                {
                    throw new Exception("DifficultyManager can only be set once!");
                }

                difficultyManager = value;
            } 
        }
        
        
    }
}