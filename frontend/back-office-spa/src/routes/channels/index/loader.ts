

export default async function loader() {
  return fetch(`/api/channels`).then(response => response.json());
}
