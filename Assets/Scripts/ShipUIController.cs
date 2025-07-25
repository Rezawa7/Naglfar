using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ShipUIController : MonoBehaviour
{
    public Slider angleSlider;
    public Slider strengthSlider;
    public Text healthText;
    public Text angleText;
    public Text strengthText;
    public Dropdown shotTypeDropdown;

    public BoatCannonController ship; // Your new ShipController (manages all 6 cannons)
    public Health shipHealth;

    void Start()
    {
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (shotTypeDropdown != null)
        {
            shotTypeDropdown.ClearOptions();
            shotTypeDropdown.AddOptions(System.Enum.GetNames(typeof(ShotType)).ToList());
            shotTypeDropdown.onValueChanged.AddListener(OnShotTypeChanged);
        }
    }

    void Update()
    {
        if (ship == null) return;

        ship.SetAngle(angleSlider.value);
        ship.SetStrength(strengthSlider.value);

        if (angleText) angleText.text = "Angle: " + Mathf.RoundToInt(ship.CurrentAngle);
        if (strengthText) strengthText.text = "Strength: " + Mathf.RoundToInt(ship.CurrentStrength);
        if (shipHealth != null && healthText != null)
        {
            healthText.text = "HP: " + Mathf.RoundToInt(shipHealth.currentHealth) + "/" + shipHealth.maxHealth;
        }
    }

    void OnShotTypeChanged(int index)
    {
        ship.SetShotType(index);
    }
}

