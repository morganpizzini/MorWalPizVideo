import { Catalogue } from "./EquipmentPanels";
export default function CataloguePanel(props: Parameters<typeof Catalogue>[0]) {
  return <Catalogue {...props} />;
}
