import { Inventory } from "./EquipmentPanels";
export default function InventoryPanel(props: Parameters<typeof Inventory>[0]) {
  return <Inventory {...props} />;
}
