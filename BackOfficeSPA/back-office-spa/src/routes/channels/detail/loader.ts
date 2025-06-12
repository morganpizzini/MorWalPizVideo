

export default async function loader({ params }: { params: { id: string } }) {
  return fetch(`/api/channels/${params.id}`).then(response => response.json());
}
