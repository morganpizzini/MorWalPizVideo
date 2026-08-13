import { getCalendar } from "@services/calendar";

export default async function loader() {
    const calendar = await getCalendar();
    return { calendar };
}