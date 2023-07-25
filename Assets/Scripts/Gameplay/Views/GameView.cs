using Assets.Scripts.Gameplay.Controllers;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Gameplay.Views
{
    public class GameView : MonoBehaviour
    {
        public PlayerController Player;
        private TextMeshPro _text;

        public float Speed => Player.Data.Speed;
        // Use this for initialization
        void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            _text = GetComponent<TextMeshPro>();
        }

        // Update is called once per frame
        void Update()
        {
            _text.text = "Speed: " + Speed.ToString("0.0");
        }
    }
}