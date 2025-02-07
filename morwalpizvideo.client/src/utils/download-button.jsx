const DownloadButton = ({ fileUrl,fileName,btnText }) => {
    const handleDownload = async () => {
        const response = await fetch(fileUrl);
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.setAttribute("download", "file.pdf");
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        URL.revokeObjectURL(url);
    };

    return <button className="btn btn-primary" onClick={handleDownload}>{btnText}</button>;
};

export default DownloadButton;
