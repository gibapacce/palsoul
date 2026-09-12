namespace Palsoul.Core
{
    /// <summary>
    /// Enum flags representando as aptidões de trabalho de uma criatura na base.
    /// GDD seção 5.4: toda criatura tem pelo menos 1 aptidão.
    ///
    /// Uso: WorkAffinity affinity = WorkAffinity.Mining | WorkAffinity.Transport;
    ///      bool canMine = affinity.HasFlag(WorkAffinity.Mining);
    /// </summary>
    [System.Flags]
    public enum WorkAffinity
    {
        None        = 0,
        Mining      = 1 << 0,   // Mineração — extrai pedra, minério
        Logging     = 1 << 1,   // Corte de madeira
        Harvesting  = 1 << 2,   // Colheita — fazenda
        Smelting    = 1 << 3,   // Fundição — forja
        Generating  = 1 << 4,   // Geração de energia elétrica
        Transport   = 1 << 5,   // Transporte de recursos entre edifícios
        Building    = 1 << 6,   // Construção — acelera upgrade do Ancoradouro
        Watering    = 1 << 7,   // Irrigação — plantas crescem mais rápido
    }
}
