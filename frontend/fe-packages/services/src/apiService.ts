import transportApiService from "./apiTransport";
import {
  createSponsor,
  createSponsorWithImage,
  deleteSponsor,
  fetchSponsors,
  getSponsor,
  updateSponsor,
  updateSponsorWithImage,
} from "./adminService";

export * from "./apiTransport";
export * from "./adminService";

const apiService = {
  ...transportApiService,
  fetchSponsors,
  getSponsor,
  createSponsor,
  createSponsorWithImage,
  updateSponsor,
  updateSponsorWithImage,
  deleteSponsor,
};

export default apiService;
