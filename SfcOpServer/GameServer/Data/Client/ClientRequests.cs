#pragma warning disable IDE0130

namespace SfcOpServer
{
    /*
        0x0e, 0x07, 0x00, 0x0d // MetaViewPortHandlerNameC  -> null (Q_15_3) ***
        0x0e, 0x03, 0x00, 0x00 // MetaViewPortHandlerNameC  -> D_3 (hex data)
        0x0e, 0x06, 0x00, 0x0f // MetaViewPortHandlerNameC  -> 14_F (hex icons)
        0x06, 0x02, 0x00, 0x02 // PlayerRelayC              -> 14_8 (character data)
        0x06, 0x03, 0x00, 0x04 // PlayerRelayC              -> 14_10 (character location, supply and shipyard availability)
        0x06, 0x04, 0x00, 0x05 // PlayerRelayC              -> A_5 (called in the end of a ship purchase) ***
        0x06, 0x04, 0x00, 0x06 // PlayerRelayC              -> A_5 (all ship's data, trade prices, and supply costs)
        0x06, 0x05, 0x00, 0x07 // PlayerRelayC              -> 14_12 (character prestige)
        0x06, 0x06, 0x00, 0x08 // PlayerRelayC              -> 14_13 (called in the end of a ship purchase) ***
        0x06, 0x03, 0x00, 0x03 // PlayerRelayC              -> A_5 (called in the end of a ship purchase) ***
        0x06, 0x07, 0x00, 0x0b // PlayerRelayC              -> 14_14 (character rank)
        0x06, 0x08, 0x00, 0x0c // PlayerRelayC              -> 14_15 (character awards)
        0x12, 0x03, 0x00, 0x00 // MetaClientNewsPanel       -> F_2 (news)
        0x0b, 0x02, 0x00, 0x00 // MetaClientPlayerListPanel -> 14_8 (called to add a name to the player's list) ***
        0x0b, 0x03, 0x00, 0x01 // MetaClientPlayerListPanel -> null (called to remove a name from the player's list) ***
        0x16, 0x05, 0x00, 0x0d // MetaClientShipPanel       -> null (Q_15_3) ***
        0x14, 0x04, 0x00, 0x0d // MetaClientSupplyDockPanel -> null (Q_15_3) ***
        0x14, 0x05, 0x00, 0x0e // MetaClientSupplyDockPanel -> null (Q_15_3) ***
    */

    public enum ClientRequests
    {
        MetaViewPortHandlerNameC_0x03_0x00_0x00,
        MetaViewPortHandlerNameC_0x07_0x00_0x0d,
        MetaViewPortHandlerNameC_0x06_0x00_0x0f,

        PlayerRelayC_0x02_0x00_0x02,
        PlayerRelayC_0x03_0x00_0x03,
        PlayerRelayC_0x03_0x00_0x04,
        PlayerRelayC_0x04_0x00_0x05,
        PlayerRelayC_0x04_0x00_0x06,
        PlayerRelayC_0x05_0x00_0x07,
        PlayerRelayC_0x06_0x00_0x08,
        PlayerRelayC_0x07_0x00_0x0b,
        PlayerRelayC_0x08_0x00_0x0c,

        MetaClientNewsPanel_0x03_0x00_0x00,

        MetaClientPlayerListPanel_0x02_0x00_0x00,
        MetaClientPlayerListPanel_0x03_0x00_0x01,

        MetaClientShipPanel_0x05_0x00_0x0d,

        MetaClientSupplyDockPanel_0x04_0x00_0x0d,
        MetaClientSupplyDockPanel_0x05_0x00_0x0e,

        Total
    }
}
