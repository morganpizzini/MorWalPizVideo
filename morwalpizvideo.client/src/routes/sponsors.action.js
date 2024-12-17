import { askForSponsor } from '@services/sponsors'
import { data } from "react-router";
export default async function action({
        request,
    }) {
    const formData = await request.formData();
    const email = String(formData.get("email"));
    const name = String(formData.get("name"));
    const token = String(formData.get("token"));
    const description = String(formData.get("description"));

    const errors = {};

    if (!email.includes("@")) {
        errors.email = "Indirizzo email non valido";
    }

    if (name.length == 0) {
        errors.name =
            "Inserire un nome";
    }

    if (description.length < 10) {
        errors.description =
            "Inserire una descrizione di almeno 10 caratteri";
    }

    if (Object.keys(errors).length > 0) {
        return data({ errors }, { status: 400 });
    }

    await askForSponsor(name,email,description,token);
    return data({ title: "Contatto salvato con successo!" }, { status: 201 })
}