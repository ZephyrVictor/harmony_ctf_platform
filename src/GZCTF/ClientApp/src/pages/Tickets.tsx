import {
  Badge,
  Center,
  Code,
  Grid,
  Group,
  Modal,
  Pagination,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import dayjs from 'dayjs'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { TicketAttachmentList, TicketAttachmentSummary } from '@Components/TicketAttachments'
import { TicketDescriptionPreview } from '@Components/TicketDescription'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg } from '@Utils/Shared'
import { getMyTickets, TicketDto } from '@Utils/ExtApi'
import { Role } from '@Api'

const Tickets: FC = () => {
  const { t } = useTranslation()
  const [tickets, setTickets] = useState<TicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const perPage = 20

  const [detailModal, detailModalHandlers] = useDisclosure(false)
  const [selectedTicket, setSelectedTicket] = useState<TicketDto | null>(null)

  const load = useCallback(async () => {
    try {
      const data = await getMyTickets(200, 0)
      setTickets(data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    load()
  }, [load])

  const openDetail = (ticket: TicketDto) => {
    setSelectedTicket(ticket)
    detailModalHandlers.open()
  }

  const handleAttachmentsUploaded = (attachments: TicketDto['attachments']) => {
    if (!selectedTicket) return
    setSelectedTicket({ ...selectedTicket, attachments })
    setTickets((items) => items.map((ticket) => (
      ticket.id === selectedTicket.id ? { ...ticket, attachments } : ticket
    )))
  }

  const pagedTickets = tickets.slice((page - 1) * perPage, page * perPage)

  const statusColor = (status: string) =>
    status === 'Approved' ? 'green' : status === 'Rejected' ? 'red' : 'yellow'

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <Stack gap="md">
          <Title order={4}>{t('game.content.ticket.my_tickets')}</Title>
          {tickets.length === 0 && !loading ? (
            <Center h="10rem">
              <Text c="dimmed">{t('admin.ticket.empty')}</Text>
            </Center>
          ) : (
            <>
              <ScrollArea type="never" h="calc(100vh - 20rem)">
                <Table highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('admin.ticket.col.challenge')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.description')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.status')}</Table.Th>
                      <Table.Th>{t('admin.ticket.col.time')}</Table.Th>
                      <Table.Th>{t('game.content.ticket.refund_bonus')}</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {pagedTickets.map((ticket) => (
                      <Table.Tr
                        key={ticket.id}
                        style={{ cursor: 'pointer' }}
                        onClick={() => openDetail(ticket)}
                      >
                        <Table.Td>
                          <Text size="sm">{ticket.challengeName ?? '—'}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Stack gap={4}>
                            <Text size="sm" lineClamp={2}>{ticket.description}</Text>
                            <TicketAttachmentSummary attachments={ticket.attachments} />
                          </Stack>
                        </Table.Td>
                        <Table.Td>
                          <Badge color={statusColor(ticket.status)} variant="light">
                            {ticket.status}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm">{dayjs(ticket.createdAt).format('L LT')}</Text>
                        </Table.Td>
                        <Table.Td>
                          {ticket.status === 'Approved' ? (
                            <Text size="sm" c="green">
                              +{ticket.refundScore + ticket.bonusScore}
                            </Text>
                          ) : ticket.status === 'Rejected' ? (
                            <Text size="sm" c="dimmed">{ticket.reviewComment ?? '—'}</Text>
                          ) : (
                            <Text size="sm" c="dimmed">—</Text>
                          )}
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
              <Pagination
                value={page}
                onChange={setPage}
                total={Math.ceil(tickets.length / perPage)}
              />
            </>
          )}

          <Modal
            opened={detailModal}
            onClose={detailModalHandlers.close}
            title={t('game.content.ticket.detail_title')}
            size="lg"
          >
            {selectedTicket && (
              <Stack gap="md">
                <Grid>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.challenge')}</Text>
                    <Text>{selectedTicket.challengeName ?? '—'}</Text>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.status')}</Text>
                    <Badge color={statusColor(selectedTicket.status)} variant="light" mt={4}>
                      {selectedTicket.status}
                    </Badge>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.answer')}</Text>
                    <Code block>{selectedTicket.answer ?? '—'}</Code>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.penalty_score')}</Text>
                    <Text fw="bold" c="red">-{selectedTicket.penaltyScore}</Text>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.time')}</Text>
                    <Text>{dayjs(selectedTicket.createdAt).format('L LTS')}</Text>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('game.content.ticket.submission_id')}</Text>
                    <Text>#{selectedTicket.submissionId}</Text>
                  </Grid.Col>
                  <Grid.Col span={12}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.col.description')}</Text>
                    <TicketDescriptionPreview source={selectedTicket.description} />
                  </Grid.Col>
                  <Grid.Col span={12}>
                    <TicketAttachmentList
                      attachments={selectedTicket.attachments}
                      ticketId={selectedTicket.id}
                      canUpload={selectedTicket.status === 'Pending'}
                      onUploaded={handleAttachmentsUploaded}
                    />
                  </Grid.Col>
                </Grid>

                {selectedTicket.status !== 'Pending' && (
                  <Stack gap="xs">
                    <Text size="sm" fw="bold">{t('game.content.ticket.review_result')}</Text>
                    <Grid>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.reviewer')}</Text>
                        <Text>{selectedTicket.reviewerName ?? '—'}</Text>
                      </Grid.Col>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.review_time')}</Text>
                        <Text>
                          {selectedTicket.reviewedAt
                            ? dayjs(selectedTicket.reviewedAt).format('L LTS')
                            : '—'}
                        </Text>
                      </Grid.Col>
                      {selectedTicket.status === 'Approved' && (
                        <>
                          <Grid.Col span={6}>
                            <Text size="sm" c="dimmed">{t('admin.ticket.refund_score')}</Text>
                            <Text c="green">+{selectedTicket.refundScore}</Text>
                          </Grid.Col>
                          <Grid.Col span={6}>
                            <Text size="sm" c="dimmed">{t('admin.ticket.bonus_score')}</Text>
                            <Text c="green">+{selectedTicket.bonusScore}</Text>
                          </Grid.Col>
                        </>
                      )}
                      <Grid.Col span={12}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.comment')}</Text>
                        <Text mt={4}>{selectedTicket.reviewComment || '—'}</Text>
                      </Grid.Col>
                    </Grid>
                  </Stack>
                )}

                {selectedTicket.status === 'Pending' && (
                  <Center h="4rem">
                    <Text c="dimmed">{t('game.content.ticket.pending_notice')}</Text>
                  </Center>
                )}
              </Stack>
            )}
          </Modal>
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}

export default Tickets
