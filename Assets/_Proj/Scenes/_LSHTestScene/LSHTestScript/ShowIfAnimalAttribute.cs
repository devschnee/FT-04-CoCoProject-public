using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShowIfAnimalAttribute : PropertyAttribute
{
    public AnimalType animalType;

    // animalType 이 특정 값일 때 이 필드를 보여준다캄
    public ShowIfAnimalAttribute(AnimalType animalType)
    {
        this.animalType = animalType;
    }
}
