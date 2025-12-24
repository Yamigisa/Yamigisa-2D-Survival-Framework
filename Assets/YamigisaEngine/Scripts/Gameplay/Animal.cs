using UnityEngine;
using Yamigisa;

public class Animal : MonoBehaviour
{
    [Header("Animal Data")]
    public AnimalData animalData;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

}
