using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

namespace Autohand{
    //THIS MAY NOT WORK AS A GRABBABLE AT THIS TIME - Try PhysicsGadgetSlider instead
    public class buttonScript : PhysicsGadgetConfigurableLimitReader{
        bool pressed = false;

        [Tooltip("The percentage (0-1) from the required value needed to call the event, if threshold is 0.1 OnPressed will be called at 0.9, and OnUnpressed at 0.1"), Min(0.01f)]
        public float threshold = 0.1f;
        public bool lockOnPressed = false;

        [Header("Compte enrere")]
        [Tooltip("Text TextMeshPro de la UI on es mostra el compte enrere")]
        public TMP_Text countdownText;
        [Tooltip("Segons de compte enrere abans de canviar d'escena")]
        public int countdownSeconds = 3;
        [Tooltip("Nom de l'escena a carregar (ha d'estar a Build Settings)")]
        public string sceneName;
        [Tooltip("Text que es mostra quan s'acaba el compte enrere (deixa-ho buit per no mostrar res)")]
        public string finalText = "";

        [Space]
        public UnityEvent OnPressed;
        public UnityEvent OnUnpressed;

        Vector3 startPos;
        Vector3 pressedPos;
        float pressedValue;

        Rigidbody body;
        Coroutine countdownRoutine;

        new protected void Start(){
            base.Start();
            startPos = transform.localPosition;
            body = joint.GetComponent<Rigidbody>();
        }


        protected void FixedUpdate(){
            var value = GetValue();
            if(!pressed && value+threshold >= 1) 
                Pressed();
            else if(!lockOnPressed && pressed && value-threshold <= 0)
                Unpressed();
        }


        public void Pressed() {
            pressed = true;
            pressedValue = GetValue();
            pressedPos = transform.localPosition;
            OnPressed?.Invoke();
            if(lockOnPressed)
                body.isKinematic = true;

            // Evita iniciar el compte enrere més d'una vegada
            if(countdownRoutine == null)
                countdownRoutine = StartCoroutine(CountdownAndChangeScene());
        }

        public void Unpressed(){
            pressed = false;
            OnUnpressed?.Invoke();
        }

        public void Unlock() {
            lockOnPressed = false;
            body.isKinematic = false;
            body.WakeUp();
        }

        IEnumerator CountdownAndChangeScene(){
            for(int i = countdownSeconds; i > 0; i--){
                if(countdownText != null)
                    countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            if(countdownText != null)
                countdownText.text = finalText;

            SceneManager.LoadScene(1);
        }
        
    }
}