import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import { UpdateQueryLinkDTO } from '@/models';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
    const values = Object.fromEntries(await request.formData()) as UpdateQueryLinkDTO;
    const errors: Record<string, string | string[]> = {};

    // Field validation
    if (!values.title || values.title.trim().length === 0) {
        errors['title'] = 'Title cannot be empty';
    }

    if (!values.value || values.value.trim().length === 0) {
        errors['value'] = 'Value cannot be empty';
    }

    // Return errors if any
    if (Object.keys(errors).length > 0) {
        return data({ success: false, errors }, { status: 400 });
    }

    return put(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: params.id }), values)
        .then(() => {
            return data({ success: true }, { status: 201 });
        })
        .catch(() => {
            errors['generics'] = ['API error found'];
            return data({ success: false, errors }, { status: 500 });
        });
}
