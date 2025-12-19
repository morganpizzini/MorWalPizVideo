export interface Sponsor {
  id: string;
  title: string;
  url: string;
  imgSrc: string;
  creationDateTime: string;
}

export interface CreateSponsorDTO {
  title: string;
  url: string;
  imgSrc: string;
}

export interface UpdateSponsorDTO {
  title: string;
  url: string;
  imgSrc: string;
}
