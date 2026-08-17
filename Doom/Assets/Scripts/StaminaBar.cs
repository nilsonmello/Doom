using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    public Material barMaterial; // instância única, não o material compartilhado
    public float currentStamina; // 0-1, atualizado pelo seu sistema de stamina
    private float displayedFill;
    private float trailValue;

    public float trailSpeed = 2f; // quão devagar o trail acompanha
    public float fillSpeed = 15f; // quão rápido a barra real reage

    void Update()
    {
        displayedFill = Mathf.Lerp(displayedFill, currentStamina, Time.deltaTime * fillSpeed);

        // trail só desce quando a stamina cai; sobe junto quando recupera
        if (displayedFill < trailValue)
            trailValue = Mathf.MoveTowards(trailValue, displayedFill, Time.deltaTime * trailSpeed);
        else
            trailValue = displayedFill;

        barMaterial.SetFloat("fillAmount", displayedFill);
        barMaterial.SetFloat("trailAmount", trailValue);
    }
}