import { useState } from 'react';
import Carousel from 'react-bootstrap/Carousel';

export default function ControlledCarousel({images}) {
    const [index, setIndex] = useState(0);
   

    const handleSelect = (selectedIndex) => {
        setIndex(selectedIndex);
    };

    return (
        <>
            <Carousel fade pause="hover" indicators={false} activeIndex={index} onSelect={handleSelect}>
                {images.map((image, i) => (
                    <Carousel.Item key={i}>
                        <img className="w-100 d-block" src={image.source} />
                        {/*<Carousel.Caption>*/}
                        {/*    <h3>{image.title}</h3>*/}
                        {/*    <p>{image.description}</p>*/}
                        {/*</Carousel.Caption>*/}
                    </Carousel.Item>))}
            </Carousel>
            <div className="row mt-3">
                {images.map((image, i) => (<div className="col-2 c-pointer" key={i} onClick={() => handleSelect(i)}>
                    <img className="w-100" src={image.source} alt={image.title} />
                </div>))}
            </div>
        </>
    );
}