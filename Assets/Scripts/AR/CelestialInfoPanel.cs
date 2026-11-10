using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    public class CelestialInfoPanel : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text englishText;
        [SerializeField] private Text shortText;
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Text detailText;
        [SerializeField] private Text expandLabel;
        [SerializeField] private GameObject structureButton;
        [SerializeField] private Text structureLabel;

        public void Show(CelestialBodyData data, bool canViewStructure)
        {
            if (data == null) return;
            gameObject.SetActive(true);
            Collapse();
            if (titleText != null) titleText.text = data.englishName;
            if (englishText != null) englishText.text = data.id.ToUpperInvariant();
            if (shortText != null) shortText.text = data.shortDescriptionEn;
            if (detailText != null) detailText.text = BuildDetails(data);
            if (structureButton != null) structureButton.SetActive(canViewStructure);
            SetStructureLabel("VIEW INTERIOR");
        }

        public void ToggleDetail()
        {
            if (detailRoot != null && detailRoot.activeSelf) Collapse();
            else Expand();
        }

        public void Expand()
        {
            if (detailRoot != null) detailRoot.SetActive(true);
            if (expandLabel != null) expandLabel.text = "HIDE";
        }

        public void Collapse()
        {
            if (detailRoot != null) detailRoot.SetActive(false);
            if (expandLabel != null) expandLabel.text = "DETAILS";
        }

        public void SetStructureLabel(string label)
        {
            if (structureLabel != null) structureLabel.text = label;
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private string BuildDetails(CelestialBodyData d)
        {
            return d.fullDescriptionEn + "\n\n" +
                   "Diameter: " + d.diameter + "\n" +
                   "Rotation Period: " + d.rotationPeriod + "\n" +
                   "Orbital Period: " + d.orbitalPeriod + "\n" +
                   "Age: " + d.age + "\n\n" +
                   "Key Features: " + d.characteristics;
        }
    }
}
