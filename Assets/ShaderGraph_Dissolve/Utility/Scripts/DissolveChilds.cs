using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DissolveExample
{
    public class DissolveChilds : MonoBehaviour
    {
        [Header("Dissolve Settings")]
        [SerializeField] private float dissolveDuration = 2f;
        [SerializeField] private float dissolveDelay = 0.5f;
        [SerializeField] private bool destroyOnComplete = true;

        private List<Material> materials = new List<Material>();
        private bool isDissolving = false;

        void Start()
        {
            CollectMaterials();
            SetValue(0f); // Ensure fully visible at start
        }

        private void CollectMaterials()
        {
            materials.Clear();
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Dissolve"))
                    {
                        materials.Add(mat);
                    }
                }
            }
        }

        public void StartDissolve()
        {
            if (isDissolving) return;

            if (materials.Count == 0)
                CollectMaterials();

            StartCoroutine(DissolveRoutine());
        }

        public void StartDissolve(float duration)
        {
            dissolveDuration = duration;
            StartDissolve();
        }

        private IEnumerator DissolveRoutine()
        {
            isDissolving = true;

            if (dissolveDelay > 0f)
                yield return new WaitForSeconds(dissolveDelay);

            float elapsed = 0f;

            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Clamp01(elapsed / dissolveDuration);
                SetValue(value);
                yield return null;
            }

            SetValue(1f);

            if (destroyOnComplete)
                Destroy(gameObject);
        }

        public void SetValue(float value)
        {
            foreach (var mat in materials)
            {
                mat.SetFloat("_Dissolve", value);
            }
        }

        public void ResetDissolve()
        {
            StopAllCoroutines();
            isDissolving = false;
            SetValue(0f);
        }
    }
}