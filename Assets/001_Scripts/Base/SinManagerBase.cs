using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts.Base
{
    public class Sin<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;

        protected void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else 
                Destroy(gameObject);
        }
    }
}
