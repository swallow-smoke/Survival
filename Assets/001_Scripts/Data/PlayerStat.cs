using System;
using UnityEngine;

namespace AstraNope.Data
{
    [Serializable]
    public class PlayerStat
    {
        [Header("Player Stats")] 
        [SerializeField, Range(0, 100)] private int HP = 100;
        [SerializeField, Range(0, 100)] private float stamina = 100f;
        [SerializeField, Range(0, 100)] private float hungry = 100f;
        [SerializeField, Range(0, 100)] private float water = 100f;
        [SerializeField, Range(0, 100)] private float oxygen = 100f;
        [SerializeField, Range(0, 100)] private float temp = 35f;

        [Header("Player Stats Usage")] 
        [SerializeField, Range(0, 100)] private float staminaUsage = 100f;
        [SerializeField, Range(0, 100)] private float staminaCure = 100f;
        [SerializeField, Range(0, 100)] private float hungryUsage = 100f;
        [SerializeField, Range(0, 100)] private float waterUsage = 100f;

        public void ModifyHP(int value) => HP = Mathf.Clamp(HP + value, 0, 100);
        public void SetHP(int value) => HP = Mathf.Clamp(value, 0, 100);
        public void ModifyStamina(float value) => stamina = Mathf.Clamp(stamina + value, 0, 100);
        public void ModifyHungry(float value) => hungry = Mathf.Clamp(hungry + value, 0, 100);
        public void ModifyWater(float value) => water = Mathf.Clamp(water + value, 0, 100);
        public void ModifyOxygen(float value) => oxygen = Mathf.Clamp(oxygen + value, 0, 100);
        public void ModifyTemp(float value) => temp = Mathf.Clamp(temp + value, 0, 100);
        public void ModifyStaminaUsage(float value) => staminaUsage = Mathf.Clamp(staminaUsage + value, 0, 100);
        public void ModifyStaminaCure(float value) =>  staminaCure = Mathf.Clamp(staminaCure + value, 0, 100);
        public void ModifyHungryUsage(float value) =>  hungryUsage = Mathf.Clamp(hungryUsage + value, 0, 100);
        public void ModifyWaterUsage(float value) =>  waterUsage = Mathf.Clamp(waterUsage + value, 0, 100);

        public int GetHP() => HP;
        public float GetStamina() => stamina;
        public float GetHungry() => hungry;
        public float GetWater() => water;
        public float GetOxygen() => oxygen;
        public float GetTemp() => temp;
        public float GetStaminaUsage() => staminaUsage;
        public float GetStaminaCure() => staminaCure;
        public float GetHungryUsage() => hungryUsage;
        public float GetWaterUsage() => waterUsage;

        public PlayerStat(
            int hp, 
            float stamina, 
            float hungry, 
            float water, 
            float oxygen, 
            float temp,
            float staminaUsage, 
            float  staminaCure, 
            float hungryUsage, 
            float waterUsage)
        {
            this.HP = hp;
            this.stamina = stamina;
            this.hungry = hungry;
            this.water = water;
            this.oxygen = oxygen;
            this.temp = temp;
            this.staminaUsage = staminaUsage;
            this.staminaCure = staminaCure;
            this.hungryUsage = hungryUsage;
            this.waterUsage = waterUsage;
        }
    }
}
