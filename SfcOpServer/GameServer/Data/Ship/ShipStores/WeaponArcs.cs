#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum WeaponArcs
    {
        Uninitialized = -1,

        // reloads

        OneReload, TwoReloads, ThreeReloads,

        // arcs

        Arc360, Arc240, Arc210, Arc180, Arc120,
        FA, RS, FRR, FLL,
        FH, RH, LS, FX,
        RX, RRR, LLR, FAR,
        FAL, ALL, RW, LW,
        FHR, FHL, RA, RWX,
        LWX, RP, LP, RAR,
        RAL, FLLX, FRRX, FARA,
        KFX, MLR, ESGL, LR,
        RRP, RLP, RSLF, LSRF,
        FALX, FARX, SFBL, SFBR,
    };
}
