import React from 'react';
import { Empty } from 'antd';
import { HistoryOutlined } from '@ant-design/icons';

interface EbomHistoryTabProps {
  bomVersionId: string;
}

export const EbomHistoryTab: React.FC<EbomHistoryTabProps> = () => {
  return (
    <div style={{ padding: 16 }}>
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description={
          <span>
            <HistoryOutlined style={{ marginRight: 8 }} />
            MVP позже
          </span>
        }
        style={{ marginTop: 48 }}
      />
    </div>
  );
};
