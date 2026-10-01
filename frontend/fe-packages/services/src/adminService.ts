import type {
  CreateSponsorDTO,
  Sponsor,
  UpdateSponsorDTO,
} from "@morwalpizvideo/models";
import endpoints, { ComposeUrl } from "./endpoints";
import {
  adminApiService,
  Delete,
  get,
  post,
  postFormData,
  put,
} from "./apiTransport";

export const fetchSponsors = (): Promise<Sponsor[]> =>
  adminApiService.get(endpoints.SPONSORS);

export const getSponsor = (id: string): Promise<Sponsor> =>
  get(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }));

export const createSponsor = (data: CreateSponsorDTO) =>
  post(endpoints.SPONSORS, data);

export const createSponsorWithImage = (formData: FormData) =>
  postFormData(endpoints.SPONSORS, formData);

export const updateSponsor = (id: string, data: UpdateSponsorDTO) =>
  put(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }), data);

export const updateSponsorWithImage = (id: string, formData: FormData) =>
  adminApiService.call(
    ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }),
    "PUT",
    formData,
    "",
    undefined,
    false,
    true,
  );

export const deleteSponsor = (id: string) =>
  Delete(ComposeUrl(endpoints.SPONSORS_DETAIL, { sponsorId: id }));
