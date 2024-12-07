import { askForSponsor } from '@services/sponsors'
import { data } from "react-router";
export default async function action({
        request,
    }) {
    const formData = await request.formData();
    const email = String(formData.get("email"));
    const password = String(formData.get("password"));

    const errors = {};

    if (!email.includes("@")) {
        errors.email = "Invalid email address";
    }

    if (password.length < 12) {
        errors.password =
            "Password should be at least 12 characters";
    }

    if (Object.keys(errors).length > 0) {
        return data({ errors }, { status: 400 });
    }

    return data({ title: "Hello" }, { status: 201 })
    //let result = await askForSponsor();

    //return result
    // Redirect to dashboard if validation is successful
    //return redirect("/dashboard");
}