export async function downloadFile(fileUrl: string, fileName = 'download') {
  const response = await fetch(fileUrl);
  if (!response.ok) throw new Error(`Download failed with status ${response.status}`);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);

  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

interface DownloadButtonProps {
  fileUrl: string;
  fileName?: string;
  btnText: string;
}

const DownloadButton = ({ fileUrl, fileName, btnText }: DownloadButtonProps) => {
  return (
    <button className="btn btn-primary" onClick={() => downloadFile(fileUrl, fileName)}>
      {btnText}
    </button>
  );
};

export default DownloadButton;
