import type { JSX } from 'react';
import { faFacebook, faInstagram, faTelegram, faYoutube } from '@fortawesome/free-brands-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Link2, Play } from 'lucide-react';
import { QuickLinkKind, type QuickLink } from '@morwalpizvideo/models';

export interface QuickLinkKindIconProps {
  kind: QuickLink['kind'];
}

export function QuickLinkKindIcon({ kind }: QuickLinkKindIconProps): JSX.Element {
  switch (kind) {
    case QuickLinkKind.Telegram:
      return <FontAwesomeIcon icon={faTelegram} aria-hidden="true" focusable="false" />;
    case QuickLinkKind.Instagram:
      return <FontAwesomeIcon icon={faInstagram} aria-hidden="true" focusable="false" />;
    case QuickLinkKind.Facebook:
      return <FontAwesomeIcon icon={faFacebook} aria-hidden="true" focusable="false" />;
    case QuickLinkKind.Video:
      return <Play size="1em" aria-hidden="true" focusable="false" />;
    case QuickLinkKind.YouTube:
      return <FontAwesomeIcon icon={faYoutube} aria-hidden="true" focusable="false" />;
    default:
      return <Link2 size="1em" aria-hidden="true" focusable="false" />;
  }
}
