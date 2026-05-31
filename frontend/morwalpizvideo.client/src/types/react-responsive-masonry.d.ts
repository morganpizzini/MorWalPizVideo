declare module 'react-responsive-masonry' {
    import { ReactNode, CSSProperties } from 'react';

    export interface ResponsiveMasonryProps {
        columnsCountBreakPoints?: { [key: number]: number };
        gutterBreakpoints?: { [key: number]: string };
        children?: ReactNode;
        className?: string;
        style?: CSSProperties;
    }

    export interface MasonryProps {
        columnsCount?: number;
        gutter?: string;
        gutterBreakpoints?: { [key: number]: string };
        children?: ReactNode;
        className?: string;
        style?: CSSProperties;
    }

    export const ResponsiveMasonry: React.FC<ResponsiveMasonryProps>;
    const Masonry: React.FC<MasonryProps>;
    export default Masonry;
}
